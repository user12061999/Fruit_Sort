using System.Collections.Generic;
using UnityEngine;

namespace FruitSort
{
    /// <summary>
    /// Renderer đơn giản cho conveyor:
    /// - Dựng strip theo ConveyorSpline.
    /// - Dựng connector bằng một cubic Bezier duy nhất.
    /// - Cùng một route được dùng cho mesh và movement.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(ConveyorSpline))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class ConveyorBeltRenderer : MonoBehaviour
    {
        [Header("Texture / Material")]
        public Material beltMaterial;
        public Texture texture;

        [Header("Mesh")]
        [Min(2)] public int segments = 64;
        public float zOffset = 0.05f;

        [Header("Connections")]
        public bool renderConnections = true;
        [Min(4)] public int connectionSegments = 16;
        [Min(0.05f)] public float connectionMinLead = 0.9f;
        [Min(0.5f)] public float connectionWidthFactor = 1.25f;
        [Range(0.05f, 0.5f)] public float connectionMaxProgress = 0.35f;
        [Range(0.9f, 0.9999f)] public float straightConnectionDot = 0.995f;
        [Min(0.01f)] public float connectionSnapWidthFactor = 0.08f;

        [Header("Texture mapping")]
        public float tilesAcrossWidth = 1f;

        [Header("Scroll")]
        public float scrollSpeed = 0.5f;

        [Header("Walls")]
        public bool showWalls;
        public float wallWidth = 0.15f;
        public float wallZOffset = -0.01f;
        public bool scrollWalls;
        public Material wallMaterialOuter;
        public Texture wallTextureOuter;
        public Material wallMaterialInner;
        public Texture wallTextureInner;

        sealed class RouteCache
        {
            public readonly List<Vector3> points = new List<Vector3>(32);
            public readonly List<float> distances = new List<float>(32);
            public int version = -1;
            public float sourceStartProgress;
            public float targetEndProgress;
            public float length;
        }

        readonly List<Vector3> _vertices = new List<Vector3>(512);
        readonly List<Vector2> _uvs = new List<Vector2>(512);
        readonly List<int> _beltTriangles = new List<int>(1024);
        readonly List<int> _outerTriangles = new List<int>(512);
        readonly List<int> _innerTriangles = new List<int>(512);
        readonly List<Vector3> _mainPath = new List<Vector3>(128);
        readonly List<float> _mainDistances = new List<float>(128);
        readonly List<ConveyorSpline> _targets = new List<ConveyorSpline>(4);
        readonly List<ConveyorSpline> _incomingSources = new List<ConveyorSpline>(4);
        readonly Dictionary<ConveyorSpline, RouteCache> _routes =
            new Dictionary<ConveyorSpline, RouteCache>(4);

        ConveyorSpline _conveyor;
        ConveyorConnections _connections;
        MeshFilter _meshFilter;
        MeshRenderer _meshRenderer;
        Mesh _mesh;
        Material _runtimeBeltMaterial;
        Material _runtimeOuterMaterial;
        Material _runtimeInnerMaterial;
        Material[] _scrollMaterials;
        float _scroll;
        float _runtimeVisualZOffset;
        bool _rebuildQueued;
        int _routeVersion;

        ConveyorSpline Conveyor =>
            _conveyor != null ? _conveyor : (_conveyor = GetComponent<ConveyorSpline>());

        ConveyorConnections Connections =>
            _connections != null ? _connections :
            (_connections = GetComponent<ConveyorConnections>());

        public float RuntimeVisualZOffset => _runtimeVisualZOffset;

        void OnEnable()
        {
            if (Application.isPlaying)
                _rebuildQueued = true;
            else
                RebuildMeshAndMaterials();
        }

        void OnValidate()
        {
            segments = Mathf.Max(2, segments);
            connectionSegments = Mathf.Max(4, connectionSegments);
            connectionMinLead = Mathf.Max(0.05f, connectionMinLead);
            connectionWidthFactor = Mathf.Max(0.5f, connectionWidthFactor);
            tilesAcrossWidth = Mathf.Max(0.0001f, tilesAcrossWidth);

            if (Application.isPlaying)
                _rebuildQueued = true;
            else
                RebuildMeshAndMaterials();
        }

        void OnDisable()
        {
            ReleaseScrollMaterials();
        }

        void OnDestroy()
        {
            ReleaseScrollMaterials();
            DestroyObject(_mesh);
            DestroyObject(_runtimeBeltMaterial);
            DestroyObject(_runtimeOuterMaterial);
            DestroyObject(_runtimeInnerMaterial);
        }

        public void SetRuntimeVisualZOffset(float offset, bool rebuild = true)
        {
            if (Mathf.Approximately(_runtimeVisualZOffset, offset))
                return;

            _runtimeVisualZOffset = offset;
            if (rebuild)
                BuildMesh();
        }

        public void InvalidateConnectionRoutes()
        {
            _routeVersion++;
        }

        public void RebuildMeshAndMaterials()
        {
            ReleaseScrollMaterials();
            _scroll = 0f;
            BuildMesh();
        }

        [ContextMenu("Rebuild Belt Mesh")]
        public void BuildMesh()
        {
            if (Conveyor == null)
                return;

            EnsureComponentsAndMaterials();
            InvalidateConnectionRoutes();
            ClearMeshBuffers();

            float startProgress = renderConnections ? GetIncomingTrimProgress() : 0f;
            float endProgress = renderConnections ? GetOutgoingTrimProgress() : 1f;

            if (endProgress <= startProgress + 0.001f)
            {
                float middle = (startProgress + endProgress) * 0.5f;
                startProgress = Mathf.Max(0f, middle - 0.0005f);
                endProgress = Mathf.Min(1f, middle + 0.0005f);
            }

            BuildMainPath(startProgress, endProgress);
            AppendMainConveyor();

            if (renderConnections)
                AppendOutgoingConnections();

            ApplyMesh();
        }

        void EnsureComponentsAndMaterials()
        {
            _meshFilter = _meshFilter != null ? _meshFilter : GetComponent<MeshFilter>();
            _meshRenderer = _meshRenderer != null ? _meshRenderer : GetComponent<MeshRenderer>();

            Material belt = ResolveMaterial(
                beltMaterial, ref _runtimeBeltMaterial, texture, "Conveyor Belt Runtime");

            if (showWalls)
            {
                Material outer = ResolveMaterial(
                    wallMaterialOuter, ref _runtimeOuterMaterial,
                    wallTextureOuter, "Conveyor Outer Wall Runtime");
                Material inner = ResolveMaterial(
                    wallMaterialInner, ref _runtimeInnerMaterial,
                    wallTextureInner, "Conveyor Inner Wall Runtime");
                _meshRenderer.sharedMaterials = new[] { belt, outer, inner };
            }
            else
            {
                _meshRenderer.sharedMaterials = new[] { belt };
            }
        }

        static Material ResolveMaterial(Material assigned, ref Material runtime,
            Texture assignedTexture, string runtimeName)
        {
            Material material = assigned;
            if (material == null)
            {
                if (runtime == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (shader == null) shader = Shader.Find("Unlit/Texture");
                    if (shader == null) shader = Shader.Find("Sprites/Default");
                    runtime = new Material(shader) { name = runtimeName };
                }
                material = runtime;
            }

            if (assignedTexture != null)
            {
                assignedTexture.wrapMode = TextureWrapMode.Repeat;
                material.mainTexture = assignedTexture;
            }

            return material;
        }

        void ClearMeshBuffers()
        {
            _vertices.Clear();
            _uvs.Clear();
            _beltTriangles.Clear();
            _outerTriangles.Clear();
            _innerTriangles.Clear();
            _targets.Clear();
        }

        float GetIncomingTrimProgress()
        {
            if (Conveyor.IsClosed)
                return 0f;

            ConveyorConnections.GetIncomingSources(Conveyor, _incomingSources);
            float progress = 0f;

            for (int i = 0; i < _incomingSources.Count; i++)
            {
                ConveyorSpline source = _incomingSources[i];
                ConveyorBeltRenderer sourceRenderer =
                    source != null ? source.GetComponent<ConveyorBeltRenderer>() : null;

                if (sourceRenderer == null ||
                    !sourceRenderer.TryGetOrBuildRoute(Conveyor, out RouteCache route))
                    continue;

                progress = Mathf.Max(progress, route.targetEndProgress);
            }

            return Mathf.Clamp01(progress);
        }

        float GetOutgoingTrimProgress()
        {
            if (Conveyor.IsClosed || Connections == null)
                return 1f;

            Connections.GetRenderTargets(_targets);
            float progress = 1f;

            for (int i = 0; i < _targets.Count; i++)
            {
                if (TryGetOrBuildRoute(_targets[i], out RouteCache route))
                    progress = Mathf.Min(progress, route.sourceStartProgress);
            }

            return Mathf.Clamp01(progress);
        }

        void BuildMainPath(float startProgress, float endProgress)
        {
            _mainPath.Clear();
            _mainDistances.Clear();

            int count = Mathf.Max(2, segments);
            for (int i = 0; i <= count; i++)
            {
                float progress = Mathf.Lerp(startProgress, endProgress, i / (float)count);
                _mainPath.Add(Conveyor.GetPositionOnSpline(progress, 0f));
            }

            BuildDistances(_mainPath, _mainDistances);
        }

        void AppendMainConveyor()
        {
            float halfWidth = Conveyor.HalfWidth;
            float wall = Mathf.Abs(wallWidth);

            AppendVariableStrip(
                _mainPath, _mainDistances,
                halfWidth, halfWidth, wall, wall,
                StripType.Belt, zOffset, 0f, _beltTriangles,
                Mathf.Max(0.0001f, Conveyor.beltWidth));

            if (!showWalls)
                return;

            AppendVariableStrip(
                _mainPath, _mainDistances,
                halfWidth, halfWidth, wall, wall,
                StripType.OuterWall, zOffset + wallZOffset, 0f,
                _outerTriangles, Mathf.Max(0.0001f, Conveyor.beltWidth));

            AppendVariableStrip(
                _mainPath, _mainDistances,
                halfWidth, halfWidth, wall, wall,
                StripType.InnerWall, zOffset + wallZOffset, 0f,
                _innerTriangles, Mathf.Max(0.0001f, Conveyor.beltWidth));
        }

        void AppendOutgoingConnections()
        {
            if (Connections == null)
                return;

            Connections.GetRenderTargets(_targets);
            for (int i = 0; i < _targets.Count; i++)
            {
                ConveyorSpline target = _targets[i];
                if (!TryGetOrBuildRoute(target, out RouteCache route))
                    continue;

                ConveyorBeltRenderer targetRenderer = target.GetComponent<ConveyorBeltRenderer>();
                float targetWall = targetRenderer != null && targetRenderer.showWalls
                    ? Mathf.Abs(targetRenderer.wallWidth)
                    : 0f;
                float sourceWall = showWalls ? Mathf.Abs(wallWidth) : 0f;
                float targetZ = targetRenderer != null ? targetRenderer.zOffset : zOffset;
                float beltZ = Mathf.Min(zOffset, targetZ) - 0.001f;

                AppendVariableStrip(
                    route.points, route.distances,
                    Conveyor.HalfWidth, target.HalfWidth,
                    sourceWall, targetWall,
                    StripType.Belt, beltZ, 0f, _beltTriangles,
                    Mathf.Max(0.0001f, Conveyor.beltWidth));

                if (!showWalls)
                    continue;

                float targetWallZ = targetRenderer != null
                    ? targetRenderer.zOffset + targetRenderer.wallZOffset
                    : zOffset + wallZOffset;
                float connectionWallZ = Mathf.Min(zOffset + wallZOffset, targetWallZ) - 0.002f;

                AppendVariableStrip(
                    route.points, route.distances,
                    Conveyor.HalfWidth, target.HalfWidth,
                    sourceWall, targetWall,
                    StripType.OuterWall, connectionWallZ, 0f,
                    _outerTriangles, Mathf.Max(0.0001f, Conveyor.beltWidth));

                AppendVariableStrip(
                    route.points, route.distances,
                    Conveyor.HalfWidth, target.HalfWidth,
                    sourceWall, targetWall,
                    StripType.InnerWall, connectionWallZ, 0f,
                    _innerTriangles, Mathf.Max(0.0001f, Conveyor.beltWidth));
            }
        }

        enum StripType
        {
            Belt,
            OuterWall,
            InnerWall,
        }

        void AppendVariableStrip(
            List<Vector3> centers,
            List<float> distances,
            float sourceHalfWidth,
            float targetHalfWidth,
            float sourceWallWidth,
            float targetWallWidth,
            StripType stripType,
            float visualZ,
            float startV,
            List<int> triangles,
            float sourceWidth)
        {
            if (centers == null || centers.Count < 2 || distances.Count != centers.Count)
                return;

            float totalLength = Mathf.Max(0.0001f, distances[distances.Count - 1]);
            int baseVertex = _vertices.Count;

            for (int i = 0; i < centers.Count; i++)
            {
                float blend = distances[i] / totalLength;
                float halfWidth = Mathf.Lerp(sourceHalfWidth, targetHalfWidth, blend);
                float currentWall = Mathf.Lerp(sourceWallWidth, targetWallWidth, blend);
                Vector3 tangent = GetPathTangent(centers, i);
                Vector3 normal = new Vector3(-tangent.y, tangent.x, 0f);

                GetStripOffsets(stripType, halfWidth, currentWall,
                    out float offsetA, out float offsetB, out float uA, out float uB,
                    sourceWidth);

                Vector3 a = centers[i] + normal * offsetA;
                Vector3 b = centers[i] + normal * offsetB;
                a.z += visualZ + _runtimeVisualZOffset;
                b.z += visualZ + _runtimeVisualZOffset;

                _vertices.Add(transform.InverseTransformPoint(a));
                _vertices.Add(transform.InverseTransformPoint(b));

                float v = startV + distances[i] * tilesAcrossWidth / sourceWidth;
                _uvs.Add(new Vector2(uA, v));
                _uvs.Add(new Vector2(uB, v));
            }

            for (int i = 0; i < centers.Count - 1; i++)
            {
                int vertex = baseVertex + i * 2;
                triangles.Add(vertex);
                triangles.Add(vertex + 2);
                triangles.Add(vertex + 1);
                triangles.Add(vertex + 1);
                triangles.Add(vertex + 2);
                triangles.Add(vertex + 3);
            }
        }

        void GetStripOffsets(StripType stripType, float halfWidth, float currentWall,
            out float offsetA, out float offsetB, out float uA, out float uB,
            float sourceWidth)
        {
            switch (stripType)
            {
                case StripType.OuterWall:
                    offsetA = halfWidth + currentWall;
                    offsetB = halfWidth;
                    uA = currentWall * tilesAcrossWidth / sourceWidth;
                    uB = 0f;
                    break;

                case StripType.InnerWall:
                    offsetA = -halfWidth;
                    offsetB = -halfWidth - currentWall;
                    uA = 0f;
                    uB = currentWall * tilesAcrossWidth / sourceWidth;
                    break;

                default:
                    offsetA = halfWidth;
                    offsetB = -halfWidth;
                    uA = tilesAcrossWidth;
                    uB = 0f;
                    break;
            }
        }

        static Vector3 GetPathTangent(List<Vector3> path, int index)
        {
            Vector3 tangent;
            if (index <= 0)
                tangent = path[1] - path[0];
            else if (index >= path.Count - 1)
                tangent = path[path.Count - 1] - path[path.Count - 2];
            else
                tangent = path[index + 1] - path[index - 1];

            tangent.z = 0f;
            return tangent.sqrMagnitude > 0.000001f ? tangent.normalized : Vector3.right;
        }

        void ApplyMesh()
        {
            if (_mesh == null)
                _mesh = new Mesh { name = "Conveyor Belt Mesh" };

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetUVs(0, _uvs);
            _mesh.subMeshCount = showWalls ? 3 : 1;
            _mesh.SetTriangles(_beltTriangles, 0);

            if (showWalls)
            {
                _mesh.SetTriangles(_outerTriangles, 1);
                _mesh.SetTriangles(_innerTriangles, 2);
            }

            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            _meshFilter.sharedMesh = _mesh;
        }

        bool TryGetOrBuildRoute(ConveyorSpline target, out RouteCache route)
        {
            route = null;
            if (!CanConnectTo(target))
                return false;

            if (!_routes.TryGetValue(target, out route))
            {
                route = new RouteCache();
                _routes.Add(target, route);
            }

            if (route.version == _routeVersion)
                return route.length > 0.0001f;

            BuildRoute(target, route);
            route.version = _routeVersion;
            return route.length > 0.0001f;
        }

        bool CanConnectTo(ConveyorSpline target)
        {
            return Conveyor != null && target != null && target != Conveyor &&
                   !Conveyor.IsClosed && !target.IsClosed;
        }

        void BuildRoute(ConveyorSpline target, RouteCache route)
        {
            route.points.Clear();
            route.distances.Clear();
            route.length = 0f;

            float sourceLength = Mathf.Max(0.01f, Conveyor.GetSplineLength());
            float targetLength = Mathf.Max(0.01f, target.GetSplineLength());
            ConveyorBeltRenderer targetRenderer = target.GetComponent<ConveyorBeltRenderer>();

            // Source trim chỉ phụ thuộc source; target trim chỉ phụ thuộc target.
            // Nhờ đó mọi nhánh rời cùng một source có chung điểm bắt đầu và mọi source
            // đi vào cùng một target có chung điểm kết thúc.
            float sourceLead = Mathf.Clamp(
                Mathf.Max(connectionMinLead, Conveyor.HalfWidth * connectionWidthFactor),
                0.01f,
                Mathf.Max(0.01f, sourceLength * connectionMaxProgress));

            float targetMinLead = targetRenderer != null
                ? targetRenderer.connectionMinLead
                : connectionMinLead;
            float targetWidthFactor = targetRenderer != null
                ? targetRenderer.connectionWidthFactor
                : connectionWidthFactor;
            float targetMaxProgress = targetRenderer != null
                ? targetRenderer.connectionMaxProgress
                : connectionMaxProgress;
            float targetLead = Mathf.Clamp(
                Mathf.Max(targetMinLead, target.HalfWidth * targetWidthFactor),
                0.01f,
                Mathf.Max(0.01f, targetLength * targetMaxProgress));

            route.sourceStartProgress = Mathf.Clamp01(1f - sourceLead / sourceLength);
            route.targetEndProgress = Mathf.Clamp01(targetLead / targetLength);

            Vector3 start = Conveyor.GetPositionOnSpline(route.sourceStartProgress, 0f);
            Vector3 end = target.GetPositionOnSpline(route.targetEndProgress, 0f);
            Vector3 startTangent = Conveyor.GetTangent(route.sourceStartProgress);
            Vector3 endTangent = target.GetTangent(route.targetEndProgress);

            Vector3 sourceEnd = Conveyor.GetPositionOnSpline(1f, 0f);
            Vector3 targetStart = target.GetPositionOnSpline(0f, 0f);
            float snapDistance = Mathf.Max(
                0.01f,
                Mathf.Max(Conveyor.beltWidth, target.beltWidth) * connectionSnapWidthFactor);
            bool sameJunction = Vector2.Distance(sourceEnd, targetStart) <= snapDistance;
            bool straight = Vector3.Dot(startTangent, endTangent) >= straightConnectionDot;

            float chord = Vector2.Distance(start, end);
            float handle = Mathf.Max(chord * 0.35f, Mathf.Max(sourceLead, targetLead) * 0.35f);
            Vector3 controlA = start + startTangent * handle;
            Vector3 controlB = end - endTangent * handle;
            int count = Mathf.Max(4, connectionSegments);

            for (int i = 0; i <= count; i++)
            {
                float t = i / (float)count;
                Vector3 point = straight && sameJunction
                    ? Vector3.LerpUnclamped(start, end, t)
                    : EvaluateCubic(start, controlA, controlB, end, t);
                AddUnique(route.points, point);
            }

            BuildDistances(route.points, route.distances);
            route.length = route.distances.Count > 0
                ? route.distances[route.distances.Count - 1]
                : 0f;
        }

        static Vector3 EvaluateCubic(Vector3 start, Vector3 controlA,
            Vector3 controlB, Vector3 end, float t)
        {
            float inverse = 1f - t;
            return inverse * inverse * inverse * start +
                   3f * inverse * inverse * t * controlA +
                   3f * inverse * t * t * controlB +
                   t * t * t * end;
        }

        static void AddUnique(List<Vector3> points, Vector3 point)
        {
            if (points.Count == 0 ||
                (points[points.Count - 1] - point).sqrMagnitude > 0.00000001f)
                points.Add(point);
        }

        static void BuildDistances(List<Vector3> points, List<float> distances)
        {
            distances.Clear();
            if (points.Count == 0)
                return;

            distances.Add(0f);
            float total = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                total += Vector2.Distance(points[i - 1], points[i]);
                distances.Add(total);
            }
        }

        public bool TryGetConnectionRoute(ConveyorSpline target,
            out float sourceStartProgress,
            out float targetEndProgress,
            out float routeLength)
        {
            sourceStartProgress = 1f;
            targetEndProgress = 0f;
            routeLength = 0f;

            if (!TryGetOrBuildRoute(target, out RouteCache route))
                return false;

            sourceStartProgress = route.sourceStartProgress;
            targetEndProgress = route.targetEndProgress;
            routeLength = route.length;
            return true;
        }

        public bool TrySampleConnectionRoute(ConveyorSpline target,
            float distance,
            float lateralOffset,
            out Vector3 position,
            out Vector3 tangent)
        {
            position = transform.position;
            tangent = Vector3.right;

            if (!TryGetOrBuildRoute(target, out RouteCache route) || route.points.Count < 2)
                return false;

            distance = Mathf.Clamp(distance, 0f, route.length);
            int segment = 0;
            while (segment < route.distances.Count - 2 &&
                   route.distances[segment + 1] < distance)
                segment++;

            float blend = Mathf.InverseLerp(
                route.distances[segment],
                route.distances[segment + 1],
                distance);
            Vector3 a = route.points[segment];
            Vector3 b = route.points[segment + 1];
            tangent = b - a;
            tangent.z = 0f;
            tangent = tangent.sqrMagnitude > 0.000001f
                ? tangent.normalized
                : Vector3.right;

            Vector3 center = Vector3.LerpUnclamped(a, b, blend);
            Vector3 normal = new Vector3(-tangent.y, tangent.x, 0f);
            position = center + normal * lateralOffset;
            return true;
        }

        void Update()
        {
            if (_rebuildQueued)
            {
                _rebuildQueued = false;
                RebuildMeshAndMaterials();
            }

            if (!Application.isPlaying || _meshRenderer == null)
                return;

            if (_scrollMaterials == null)
                _scrollMaterials = _meshRenderer.materials;

            _scroll += scrollSpeed * Time.deltaTime;
            for (int i = 0; i < _scrollMaterials.Length; i++)
            {
                Material material = _scrollMaterials[i];
                if (material == null || (i > 0 && !scrollWalls))
                    continue;

                Vector2 offset = material.mainTextureOffset;
                offset.y = -_scroll;
                material.mainTextureOffset = offset;
            }
        }

        void ReleaseScrollMaterials()
        {
            if (_scrollMaterials == null)
                return;

            for (int i = 0; i < _scrollMaterials.Length; i++)
                DestroyObject(_scrollMaterials[i]);

            _scrollMaterials = null;
        }

        static void DestroyObject(Object value)
        {
            if (value == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(value);
            else
                Object.DestroyImmediate(value);
        }
    }
}
