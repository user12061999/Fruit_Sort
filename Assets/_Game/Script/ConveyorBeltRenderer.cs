using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FruitSort
{
    /// <summary>
    /// Sinh một dải mesh chạy dọc theo <see cref="ConveyorSpline"/> với bề rộng = beltWidth,
    /// dán texture lên dải đó. Texture được map sao cho tỉ lệ ô luôn vuông theo CHIỀU RỘNG băng
    /// (đổi beltWidth -> texture co/giãn theo), và CUỘN dọc theo hướng băng chuyền để mô phỏng
    /// băng đang chạy.
    /// Tuỳ chọn dựng thêm THÀNH TRONG / THÀNH NGOÀI: hai dải viền dọc 2 mép băng, mỗi thành có
    /// material/texture riêng (submesh riêng). Thành đứng yên (không cuộn) như khung băng.
    /// Gắn CHUNG GameObject với ConveyorSpline (SplineContainer).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(ConveyorSpline))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ConveyorBeltRenderer : MonoBehaviour
    {
        [Header("Texture / Material")]
        [Tooltip("Material dùng cho băng. Để trống sẽ tự tạo material Unlit từ 'texture' bên dưới.")]
        public Material beltMaterial;
        [Tooltip("Texture băng chuyền (nên đặt Wrap Mode = Repeat). Dùng khi không gán beltMaterial.")]
        public Texture texture;

        [Header("Mesh")]
        [Tooltip("Số đoạn chia dọc spline. Càng cao càng mượt theo đường cong.")]
        [Min(2)] public int segments = 64;
        [Tooltip("Đẩy mesh ra sau dot một chút theo trục Z (world) để không che dot.")]
        public float zOffset = 0.05f;

        [Header("Kết nối giữa conveyor")]
        [Tooltip("Tự dựng một dải mesh nối từ cuối conveyor này sang đầu conveyor kế tiếp. " +
                 "Cần bật để các kết nối vuông góc không bị khuyết phần góc ngoài.")]
        public bool renderConnections = true;
        [Tooltip("Số đoạn chia của mesh bo tại điểm nối. 16-24 thường đủ mượt.")]
        [Min(4)] public int connectionSegments = 24;
        [Tooltip("Bán kính bo tối thiểu tại điểm nối (world unit). Với góc 90 độ, đây cũng là khoảng trim trên mỗi conveyor.")]
        [Min(0.05f)] public float connectionMinLead = 0.9f;
        [Tooltip("Bán kính bo tự động theo NỬA bề rộng băng. 1.1-1.5 thường cho mép trong đẹp và không tự cắt.")]
        [Min(0.5f)] public float connectionWidthFactor = 1.25f;
        [Tooltip("Giới hạn phần trăm chiều dài spline được trim để tạo đoạn nối.")]
        [Range(0.05f, 0.5f)] public float connectionMaxProgress = 0.35f;
        [Tooltip("Hai hướng có dot lớn hơn giá trị này được coi là nối thẳng. Nối thẳng không tạo disk/round patch nên không bị phình.")]
        [Range(0.9f, 0.9999f)] public float straightConnectionDot = 0.995f;
        [Tooltip("Khoảng lệch endpoint tối đa vẫn được coi là cùng một junction, tính theo tỉ lệ bề rộng lớn nhất.")]
        [Min(0.01f)] public float connectionSnapWidthFactor = 0.08f;

        [Header("Texture mapping")]
        [Tooltip("Số lần lặp texture theo CHIỀU RỘNG băng. Tiling dọc tự tính để giữ ô vuông.")]
        public float tilesAcrossWidth = 1f;

        [Header("Cuộn (scroll)")]
        [Tooltip("Tốc độ cuộn texture dọc băng (UV/giây). Dương = chạy theo chiều dot đi (t:0->1).")]
        public float scrollSpeed = 0.5f;

        [Header("Thành băng (tường viền 2 mép)")]
        [Tooltip("Bật để dựng thêm thành trong & thành ngoài dọc 2 mép băng.")]
        public bool showWalls = false;
        [Tooltip("Bề rộng thành (world), đo từ mép băng ra NGOÀI. Âm = lấn vào trong băng.")]
        public float wallWidth = 0.15f;
        [Tooltip("Lệch Z của thành so với băng (âm = nổi lên trước băng/dot, dương = ra sau).")]
        public float wallZOffset = -0.01f;
        [Tooltip("Bật: thành cũng cuộn theo băng. Tắt (mặc định): thành đứng yên như khung.")]
        public bool scrollWalls = false;

        [Tooltip("Material THÀNH NGOÀI (mép +halfW). Trống = tạo Unlit từ wallTextureOuter.")]
        public Material wallMaterialOuter;
        [Tooltip("Texture THÀNH NGOÀI (Wrap = Repeat). Dùng khi không gán wallMaterialOuter.")]
        public Texture wallTextureOuter;

        [Tooltip("Material THÀNH TRONG (mép -halfW). Trống = tạo Unlit từ wallTextureInner.")]
        public Material wallMaterialInner;
        [Tooltip("Texture THÀNH TRONG (Wrap = Repeat). Dùng khi không gán wallMaterialInner.")]
        public Texture wallTextureInner;

        ConveyorSpline _conveyor;
        MeshFilter _mf;
        MeshRenderer _mr;
        Mesh _mesh;
        Material _runtimeMat;        // material băng tự tạo (nếu có) để dọn dẹp
        Material _runtimeMatOuter;   // material thành ngoài tự tạo
        Material _runtimeMatInner;   // material thành trong tự tạo
        Material[] _scrollMats;      // material instances dùng để cuộn lúc play
        float _scroll;
        float _vTiles = 1f;
        float _runtimeVisualZOffset;
        bool _rebuildQueued;         // sửa field trên Inspector LÚC PLAY -> rebuild ở Update kế tiếp

        public float RuntimeVisualZOffset => _runtimeVisualZOffset;

        public void SetRuntimeVisualZOffset(float offset, bool rebuild = true)
        {
            if (Mathf.Approximately(_runtimeVisualZOffset, offset)) return;
            _runtimeVisualZOffset = offset;
            if (rebuild) BuildMesh();
        }

        // Bộ đệm dựng mesh (tái dùng để giảm GC).
        readonly List<Vector3> _verts = new List<Vector3>();
        readonly List<Vector2> _uvs = new List<Vector2>();
        readonly List<int> _triBelt = new List<int>();
        readonly List<int> _triOuter = new List<int>();
        readonly List<int> _triInner = new List<int>();

        readonly List<Vector3> _connectionPath = new List<Vector3>(64);
        readonly List<float> _connectionDistances = new List<float>(64);
        readonly List<Vector3> _stripPath = new List<Vector3>(1024);
        readonly List<ConnectionGeometry> _outgoingConnections = new List<ConnectionGeometry>(4);
        readonly Dictionary<ConveyorSpline, ConnectionRouteCache> _connectionRoutes =
            new Dictionary<ConveyorSpline, ConnectionRouteCache>(4);
        int _connectionRouteVersion;

        sealed class ConnectionRouteCache
        {
            public readonly List<Vector3> points = new List<Vector3>(64);
            public readonly List<float> distances = new List<float>(64);
            public int version = -1;
            public float sourceStartProgress;
            public float targetEndProgress;
            public float length;
        }

        struct ConnectionGeometry
        {
            public ConveyorSpline target;
            public ConveyorBeltRenderer targetRenderer;
            public bool straight;
            public bool exactFillet;
            public Vector3 sourceEnd;
            public Vector3 targetStart;
            public Vector3 junction;
            public Vector3 sourceDirection;
            public Vector3 targetDirection;
            public Vector3 sourceTangentPoint;
            public Vector3 targetTangentPoint;
            public Vector3 arcCenter;
            public float turnAngle;
            public float turnSign;
            public float sourceTrimProgress;
            public float targetTrimProgress;
        }

        const float JunctionZOffset = -0.01f;
        const float RuleTileDirectionDotThreshold = 0.92f;

        ConveyorSpline Conveyor
        {
            get { if (_conveyor == null) _conveyor = GetComponent<ConveyorSpline>(); return _conveyor; }
        }

        void OnEnable()
        {
            RebuildMeshAndMaterials();
        }

        void OnValidate()
        {
            if (this == null) return;

            // Đang PLAY: không đụng Renderer/material ngay trong OnValidate (Unity cấm/cảnh báo).
            // Đặt cờ để Update frame kế tiếp rebuild mesh + nạp lại material.
            if (Application.isPlaying)
            {
                _rebuildQueued = true;
                return;
            }

            // Edit mode: rebuild ngay khi sửa field trên Inspector (segments, zOffset, tiles...).
            BuildMesh();
#if UNITY_EDITOR
            SceneView.RepaintAll();
#endif
        }

        void OnDisable()
        {
            // Dọn material/mesh tạo runtime để tránh leak.
            if (Application.isPlaying)
            {
                ReleaseScrollMaterialInstances();
                if (_runtimeMat != null) Destroy(_runtimeMat);
                if (_runtimeMatOuter != null) Destroy(_runtimeMatOuter);
                if (_runtimeMatInner != null) Destroy(_runtimeMatInner);
            }
        }

        /// <summary>
        /// Nạp lại material từ prefab/config và dựng mesh theo spline hiện tại.
        /// Gọi sau khi LevelBuilder đã áp knots, độ rộng và cấu hình bo góc.
        /// </summary>
        public void RebuildMeshAndMaterials()
        {
            ReleaseScrollMaterialInstances();
            _scroll = 0f;
            BuildMesh();
        }

        void ReleaseScrollMaterialInstances()
        {
            if (_scrollMats == null) return;

            foreach (Material material in _scrollMats)
            {
                if (material == null) continue;
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }

            _scrollMats = null;
        }

        void EnsureComponents()
        {
            if (_mf == null) _mf = GetComponent<MeshFilter>();
            if (_mr == null) _mr = GetComponent<MeshRenderer>();

            // Material băng (submesh 0).
            Material belt = ResolveMaterial(beltMaterial, ref _runtimeMat, texture, "ConveyorBeltMat (runtime)");

            if (showWalls)
            {
                Material outer = ResolveMaterial(wallMaterialOuter, ref _runtimeMatOuter, wallTextureOuter, "ConveyorWallOuterMat (runtime)");
                Material inner = ResolveMaterial(wallMaterialInner, ref _runtimeMatInner, wallTextureInner, "ConveyorWallInnerMat (runtime)");
                _mr.sharedMaterials = new[] { belt, outer, inner };
            }
            else
            {
                _mr.sharedMaterials = new[] { belt };
            }
        }

        // Trả về material để dùng: ưu tiên material gán sẵn, nếu trống thì tạo Unlit từ texture.
        Material ResolveMaterial(Material assigned, ref Material runtimeSlot, Texture tex, string runtimeName)
        {
            if (assigned != null)
            {
                if (tex != null)
                {
                    tex.wrapMode = TextureWrapMode.Repeat;
                    assigned.mainTexture = tex;
                }
                return assigned;
            }

            if (runtimeSlot == null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Unlit/Texture");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                runtimeSlot = new Material(sh) { name = runtimeName };
            }
            if (tex != null)
            {
                tex.wrapMode = TextureWrapMode.Repeat;
                runtimeSlot.mainTexture = tex;
            }
            return runtimeSlot;
        }

        /// <summary>Dựng lại mesh dải dọc spline (kèm thành nếu bật). Gọi lại nếu spline/bề rộng thay đổi.</summary>
        [ContextMenu("Rebuild Belt Mesh")]
        public void BuildMesh()
        {
            if (Conveyor == null) return;
            EnsureComponents();
            _connectionRouteVersion++;

            int n = Mathf.Max(2, segments);
            float halfW = Conveyor.HalfWidth;
            float width = Mathf.Max(1e-4f, Conveyor.beltWidth);
            float length = Conveyor.GetSplineLength();

            _vTiles = tilesAcrossWidth * (length / width);
            float wallTiles = tilesAcrossWidth * (Mathf.Abs(wallWidth) / width);

            _verts.Clear(); _uvs.Clear();
            _triBelt.Clear(); _triOuter.Clear(); _triInner.Clear();

            CollectOutgoingConnections(_outgoingConnections);
            float startProgress = renderConnections ? GetIncomingTrimProgress(Conveyor) : 0f;
            float endTrimProgress = 0f;
            if (renderConnections)
            {
                for (int i = 0; i < _outgoingConnections.Count; i++)
                {
                    ConnectionGeometry geometry = _outgoingConnections[i];
                    if (!geometry.straight)
                        endTrimProgress = Mathf.Max(endTrimProgress, geometry.sourceTrimProgress);
                }
            }

            float endProgress = 1f - endTrimProgress;
            if (endProgress < startProgress + 0.001f)
            {
                float middle = (startProgress + endProgress) * 0.5f;
                startProgress = Mathf.Max(0f, middle - 0.0005f);
                endProgress = Mathf.Min(1f, middle + 0.0005f);
            }
            // Mesh chính được trim tại junction. Connector sẽ điền chính xác phần đã trim,
            // vì vậy không còn hai strip đầy đủ chồng lên nhau và làm góc bị phình.
            AppendStripRange(n, startProgress, endProgress, +halfW, -halfW,
                zOffset, tilesAcrossWidth, 0f, _triBelt);

            if (showWalls)
            {
                AppendStripRange(n, startProgress, endProgress,
                    +halfW + wallWidth, +halfW,
                    zOffset + wallZOffset, wallTiles, 0f, _triOuter);
                AppendStripRange(n, startProgress, endProgress,
                    -halfW, -halfW - wallWidth,
                    zOffset + wallZOffset, 0f, wallTiles, _triInner);
            }

            if (renderConnections)
                AppendOutgoingConnections(_outgoingConnections, endTrimProgress, width);

            if (showWalls)
                AppendMultiIncomingJunction(width);

            // Runtime layering áp trực tiếp lên local vertex. Cách này không phụ thuộc Z của knot
            // và không thay đổi Transform/LUT world-space của ConveyorSpline.
            if (!Mathf.Approximately(_runtimeVisualZOffset, 0f))
            {
                for (int i = 0; i < _verts.Count; i++)
                {
                    Vector3 vertex = _verts[i];
                    vertex.z += _runtimeVisualZOffset;
                    _verts[i] = vertex;
                }
            }

            if (_mesh == null) _mesh = new Mesh { name = "ConveyorBeltMesh" };
            _mesh.Clear();
            _mesh.SetVertices(_verts);
            _mesh.SetUVs(0, _uvs);

            _mesh.subMeshCount = showWalls ? 3 : 1;
            _mesh.SetTriangles(_triBelt, 0);
            if (showWalls)
            {
                _mesh.SetTriangles(_triOuter, 1);
                _mesh.SetTriangles(_triInner, 2);
            }

            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            _mf.sharedMesh = _mesh;
        }

        // Thêm 1 dải dọc spline giữa 2 mức lệch ngang latA (UV u = uA) và latB (UV u = uB).
        // Đỉnh và tam giác nối tiếp vào _verts/_uvs và list tam giác của submesh tương ứng.
        void AppendStripRange(int n, float startProgress, float endProgress,
            float latA, float latB, float zoff, float uA, float uB, List<int> tris)
        {
            int baseIndex = _verts.Count;
            startProgress = Mathf.Clamp01(startProgress);
            endProgress = Mathf.Clamp01(endProgress);

            _stripPath.Clear();
            for (int i = 0; i <= n; i++)
            {
                float t = Mathf.Lerp(startProgress, endProgress, i / (float)n);
                _stripPath.Add(Conveyor.GetPositionOnSpline(t, 0f));
            }

            for (int i = 0; i <= n; i++)
            {
                float normalized = i / (float)n;
                float t = Mathf.Lerp(startProgress, endProgress, normalized);

                Vector3 center = _stripPath[i];
                Vector3 tangent;
                if (i == 0) tangent = _stripPath[1] - _stripPath[0];
                else if (i == n) tangent = _stripPath[n] - _stripPath[n - 1];
                else tangent = _stripPath[i + 1] - _stripPath[i - 1];
                tangent.z = 0f;
                if (tangent.sqrMagnitude < 1e-6f) tangent = Vector3.right;
                else tangent.Normalize();
                Vector3 normal = new Vector3(-tangent.y, tangent.x, 0f);
                float clearance = Mathf.Max(0.01f, Conveyor.beltWidth * 0.03f);
                float safeLatA = ClampOffsetAgainstPathCurvature(
                    _stripPath, i, normal, latA, clearance);
                float safeLatB = ClampOffsetAgainstPathCurvature(
                    _stripPath, i, normal, latB, clearance);
                Vector3 wa = center + normal * safeLatA;
                Vector3 wb = center + normal * safeLatB;
                wa.z += zoff; wb.z += zoff;

                _verts.Add(transform.InverseTransformPoint(wa));
                _verts.Add(transform.InverseTransformPoint(wb));

                float v = t * _vTiles;
                _uvs.Add(new Vector2(uA, v));
                _uvs.Add(new Vector2(uB, v));
            }

            for (int i = 0; i < n; i++)
            {
                int vi = baseIndex + i * 2;
                AppendStripTriangle(tris, vi, vi + 2, vi + 1);
                AppendStripTriangle(tris, vi + 1, vi + 2, vi + 3);
            }
        }

        void AppendStripTriangle(List<int> triangles, int a, int b, int c)
        {
            // Mặt belt luôn kín. Wall ở bán kính nhỏ hơn wallWidth có thể tự cắt và đảo winding;
            // bỏ riêng tam giác đảo để tránh quạt đen, thay vì kéo rail xuyên qua tâm góc bo.
            if (!ReferenceEquals(triangles, _triBelt))
            {
                Vector3 ab = _verts[b] - _verts[a];
                Vector3 ac = _verts[c] - _verts[a];
                if (Vector3.Cross(ab, ac).z >= -1e-8f) return;
            }

            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }

        void CollectOutgoingConnections(List<ConnectionGeometry> output)
        {
            output.Clear();
            if (Conveyor.IsClosed) return;

            ConveyorConnections connections = GetComponent<ConveyorConnections>();
            if (connections == null || connections.next == null) return;

            ConveyorSpline activeTarget = null;
            ConveyorSwitch routeSwitch = GetComponent<ConveyorSwitch>();
            if (Application.isPlaying && routeSwitch != null)
                routeSwitch.TryGetActiveNext(out activeTarget);

            for (int i = 0; i < connections.next.Count; i++)
            {
                ConveyorSpline target = connections.next[i];
                if (target == null || target == Conveyor) continue;
                if (activeTarget != null && target != activeTarget) continue;

                bool duplicate = false;
                for (int j = 0; j < output.Count; j++)
                {
                    if (output[j].target == target) { duplicate = true; break; }
                }
                if (duplicate) continue;

                if (TryBuildConnectionGeometry(Conveyor, target, out ConnectionGeometry geometry))
                    output.Add(geometry);
            }
        }

        void AppendOutgoingConnections(List<ConnectionGeometry> geometries,
            float sharedSourceTrimProgress, float sourceWidth)
        {
            for (int i = 0; i < geometries.Count; i++)
            {
                ConnectionGeometry geometry = geometries[i];
                float sharedTargetTrimProgress = Mathf.Max(
                    GetIncomingTrimProgress(geometry.target),
                    GetSwitchOwnedTargetProgress(geometry.target));
                AppendConnectionPath(geometry, sharedSourceTrimProgress,
                    sharedTargetTrimProgress, sourceWidth);
            }
        }

        float GetSwitchOwnedTargetProgress(ConveyorSpline target)
        {
            if (!Application.isPlaying || target == null) return 0f;

            ConveyorSwitch routeSwitch = GetComponent<ConveyorSwitch>();
            if (routeSwitch == null ||
                !routeSwitch.TryGetActiveNext(out ConveyorSpline active) || active != target)
                return 0f;

            return routeSwitch.GetActiveBranchOwnedProgress(target);
        }

        float GetStableConnectionRouteTargetProgress(ConveyorSpline target)
        {
            if (target == null) return 0f;

            ConveyorSwitch routeSwitch = GetComponent<ConveyorSwitch>();
            ConveyorConnections connections = GetComponent<ConveyorConnections>();
            if (routeSwitch == null || routeSwitch.ValidBranchCount < 2 ||
                connections == null || connections.next == null ||
                !connections.next.Contains(target))
                return GetSwitchOwnedTargetProgress(target);

            // Movement route của mọi nhánh switch luôn kết thúc sau phần hai knot mà source
            // sở hữu. Vì vậy route của dot đang chạy không đổi hình khi nhánh đó vừa inactive.
            return routeSwitch.GetActiveBranchOwnedProgress(target);
        }

        float GetIncomingTrimProgress(ConveyorSpline target)
        {
            if (target == null || target.IsClosed) return 0f;

            float trimProgress = 0f;
            ConveyorConnections[] allConnections = Object.FindObjectsByType<ConveyorConnections>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            for (int i = 0; i < allConnections.Length; i++)
            {
                ConveyorConnections connection = allConnections[i];
                if (connection == null || connection.next == null ||
                    !connection.next.Contains(target)) continue;

                ConveyorSpline source = connection.GetComponent<ConveyorSpline>();
                if (source == null || source == target || source.IsClosed) continue;

                // Chỉ nhánh active bỏ đoạn knot 0 -> knot 1 để conveyor gốc vẽ thay.
                // Nhánh inactive giữ nguyên đoạn này và chỉ lùi Z, nhờ đó không xuất hiện khe hở.
                if (Application.isPlaying)
                {
                    trimProgress = Mathf.Max(trimProgress,
                        GetSwitchBranchTrimProgress(source, target));
                }
                if (!IsRuntimeRouteActive(source, target)) continue;

                ConveyorBeltRenderer sourceRenderer = source.GetComponent<ConveyorBeltRenderer>();
                if (sourceRenderer == null) continue;

                trimProgress = Mathf.Max(trimProgress,
                    sourceRenderer.GetSwitchOwnedTargetProgress(target));
                if (sourceRenderer.TryBuildConnectionGeometry(source, target,
                        out ConnectionGeometry geometry) && !geometry.straight)
                {
                    trimProgress = Mathf.Max(trimProgress, geometry.targetTrimProgress);
                }
            }

            return Mathf.Clamp01(trimProgress);
        }

        static float GetSwitchBranchTrimProgress(ConveyorSpline source, ConveyorSpline target)
        {
            if (source == null || target == null) return 0f;

            ConveyorSwitch routeSwitch = source.GetComponent<ConveyorSwitch>();
            if (routeSwitch == null || routeSwitch.ValidBranchCount < 2) return 0f;
            if (!routeSwitch.TryGetActiveNext(out ConveyorSpline active) || active != target)
                return 0f;
            return routeSwitch.GetActiveBranchOwnedProgress(target);
        }

        static bool IsRuntimeRouteActive(ConveyorSpline source, ConveyorSpline target)
        {
            if (!Application.isPlaying) return true;
            ConveyorSwitch routeSwitch = source.GetComponent<ConveyorSwitch>();
            if (routeSwitch == null || routeSwitch.ValidBranchCount < 2) return true;
            return routeSwitch.TryGetActiveNext(out ConveyorSpline active) && active == target;
        }

        bool TryBuildConnectionGeometry(ConveyorSpline source, ConveyorSpline target,
            out ConnectionGeometry geometry)
        {
            geometry = default;
            if (source == null || target == null || source == target ||
                source.IsClosed || target.IsClosed) return false;

            ConveyorBeltRenderer targetRenderer = target.GetComponent<ConveyorBeltRenderer>();
            if (targetRenderer == null) return false;

            Vector3 sourceEnd = source.GetPositionOnSpline(1f, 0f);
            Vector3 targetStart = target.GetPositionOnSpline(0f, 0f);
            Vector3 sourceDirection = source.GetTangent(1f);
            Vector3 targetDirection = target.GetTangent(0f);
            sourceDirection.z = 0f;
            targetDirection.z = 0f;
            if (sourceDirection.sqrMagnitude < 1e-6f || targetDirection.sqrMagnitude < 1e-6f)
                return false;
            sourceDirection.Normalize();
            targetDirection.Normalize();

            float directionDot = Mathf.Clamp(Vector3.Dot(sourceDirection, targetDirection), -1f, 1f);
            bool straight = directionDot >= straightConnectionDot;

            geometry.target = target;
            geometry.targetRenderer = targetRenderer;
            geometry.straight = straight;
            geometry.sourceEnd = sourceEnd;
            geometry.targetStart = targetStart;
            geometry.sourceDirection = sourceDirection;
            geometry.targetDirection = targetDirection;

            // Nối thẳng không trim và không thêm round patch. Nếu endpoint trùng nhau,
            // hai strip gặp nhau chính xác nên không cần sinh thêm geometry.
            if (straight) return true;

            float turnAngle = Mathf.Acos(directionDot);
            float tanHalf = Mathf.Tan(turnAngle * 0.5f);
            float cross = sourceDirection.x * targetDirection.y -
                          sourceDirection.y * targetDirection.x;
            // U-turn hoặc hai hướng gần song song ngược chiều không có fillet ổn định tại một junction.
            if (Mathf.Abs(cross) <= 1e-4f || tanHalf <= 1e-4f || directionDot <= -0.98f)
                return false;
            float turnSign = Mathf.Sign(cross);

            float widestHalfWidth = Mathf.Max(source.HalfWidth, target.HalfWidth);
            float desiredRadius = Mathf.Max(connectionMinLead,
                widestHalfWidth * connectionWidthFactor);

            float sourceAvailable = GetEndpointStraightLead(source, false);
            float targetAvailable = GetEndpointStraightLead(target, true);
            float maxTrimDistance = Mathf.Max(0.01f,
                Mathf.Min(sourceAvailable, targetAvailable));

            float trimDistance = desiredRadius * tanHalf;
            if (trimDistance > maxTrimDistance)
                trimDistance = maxTrimDistance;

            float radius = trimDistance / tanHalf;
            float sourceLength = Mathf.Max(0.01f, source.GetSplineLength());
            float targetLength = Mathf.Max(0.01f, target.GetSplineLength());
            float sourceTrimProgress = Mathf.Clamp(trimDistance / sourceLength,
                0.001f, connectionMaxProgress);
            float targetTrimProgress = Mathf.Clamp(trimDistance / targetLength,
                0.001f, connectionMaxProgress);

            // Re-evaluate bằng progress đã clamp để điểm trim của mesh và connector trùng tuyệt đối.
            Vector3 sourceTangentPoint = source.GetPositionOnSpline(1f - sourceTrimProgress, 0f);
            Vector3 targetTangentPoint = target.GetPositionOnSpline(targetTrimProgress, 0f);

            float snapTolerance = Mathf.Max(0.05f,
                Mathf.Max(source.beltWidth, target.beltWidth) * connectionSnapWidthFactor);
            bool snapped = Vector2.Distance(sourceEnd, targetStart) <= snapTolerance;
            Vector3 junction = snapped ? (sourceEnd + targetStart) * 0.5f : sourceEnd;

            // Với layout endpoint đã snap, dựng cung tròn fillet tiếp tuyến chính xác.
            // Nếu endpoint lệch nhiều, fallback path bên dưới dùng Hermite, không dùng disk.
            bool exactFillet = false;
            Vector3 arcCenter = Vector3.zero;
            if (snapped)
            {
                Vector3 leftNormal = new Vector3(-sourceDirection.y, sourceDirection.x, 0f);
                arcCenter = sourceTangentPoint + leftNormal * (turnSign * radius);

                Vector3 radialStart = sourceTangentPoint - arcCenter;
                float cos = Mathf.Cos(turnSign * turnAngle);
                float sin = Mathf.Sin(turnSign * turnAngle);
                Vector3 expectedArcEnd = arcCenter + new Vector3(
                    radialStart.x * cos - radialStart.y * sin,
                    radialStart.x * sin + radialStart.y * cos,
                    0f);

                // Chỉ dùng cung tròn khi cả hai tiếp điểm thực sự khớp. Nếu không,
                // fallback cubic sẽ tránh một đoạn gãy nhỏ ở cuối cung.
                exactFillet = Vector2.Distance(expectedArcEnd, targetTangentPoint) <=
                               Mathf.Max(0.01f, snapTolerance * 0.25f);
            }

            geometry.exactFillet = exactFillet;
            geometry.junction = junction;
            geometry.sourceTangentPoint = sourceTangentPoint;
            geometry.targetTangentPoint = targetTangentPoint;
            geometry.arcCenter = arcCenter;
            geometry.turnAngle = turnAngle;
            geometry.turnSign = turnSign;
            geometry.sourceTrimProgress = sourceTrimProgress;
            geometry.targetTrimProgress = targetTrimProgress;
            return true;
        }

        float GetEndpointStraightLead(ConveyorSpline conveyor, bool atStart)
        {
            float fallback = conveyor.GetSplineLength() * connectionMaxProgress;
            if (conveyor.Container == null || conveyor.Container.Spline == null ||
                conveyor.Container.Spline.Count < 2)
                return Mathf.Max(0.01f, fallback);

            // ConveyorSpline đã clamp fillet theo nửa segment ngắn nhất. Dùng lead sau clamp để
            // junction không bị ép xuống bán kính 0.01 khi cornerRadius cấu hình lớn hơn segment.
            float usable = Mathf.Max(0.01f, conveyor.GetEndpointStraightLead(atStart));
            return Mathf.Min(usable, fallback);
        }

        /// <summary>
        /// Trả thông tin path nối đang dùng để render giữa source và target. FallingPixelManager
        /// dùng đúng path này để dot không đổi pháp tuyến đột ngột tại endpoint.
        /// </summary>
        public bool TryGetConnectionRoute(ConveyorSpline target,
            out float sourceStartProgress, out float targetEndProgress, out float routeLength)
        {
            sourceStartProgress = 1f;
            targetEndProgress = 0f;
            routeLength = 0f;
            if (!TryGetOrBuildConnectionRoute(target, out ConnectionRouteCache route)) return false;

            sourceStartProgress = route.sourceStartProgress;
            targetEndProgress = route.targetEndProgress;
            routeLength = route.length;
            return true;
        }

        public bool TrySampleConnectionRoute(ConveyorSpline target, float distance,
            float lateralOffset, out Vector3 position, out Vector3 tangent)
        {
            position = default;
            tangent = Vector3.right;
            if (!TryGetOrBuildConnectionRoute(target, out ConnectionRouteCache route) ||
                route.points.Count < 2 || route.length <= 1e-5f)
                return false;

            distance = Mathf.Clamp(distance, 0f, route.length);
            int segment = 0;
            while (segment < route.distances.Count - 2 &&
                   route.distances[segment + 1] < distance)
                segment++;

            float startDistance = route.distances[segment];
            float endDistance = route.distances[segment + 1];
            float blend = Mathf.InverseLerp(startDistance, endDistance, distance);
            Vector3 start = route.points[segment];
            Vector3 end = route.points[segment + 1];
            tangent = end - start;
            tangent.z = 0f;
            if (tangent.sqrMagnitude < 1e-6f) tangent = Vector3.right;
            else tangent.Normalize();

            Vector3 center = Vector3.LerpUnclamped(start, end, blend);
            Vector3 normal = new Vector3(-tangent.y, tangent.x, 0f);
            float safeOffset = ClampOffsetAgainstPathCurvature(
                route.points, Mathf.Clamp(segment + 1, 0, route.points.Count - 1),
                normal, lateralOffset, 0.01f);
            position = center + normal * safeOffset;
            return true;
        }

        bool TryGetOrBuildConnectionRoute(ConveyorSpline target,
            out ConnectionRouteCache route)
        {
            route = null;
            if (target == null) return false;

            if (_connectionRoutes.TryGetValue(target, out route) &&
                route.version == _connectionRouteVersion)
                return route.length > 1e-5f;

            if (!TryBuildConnectionGeometry(Conveyor, target,
                    out ConnectionGeometry geometry))
                return false;

            if (route == null)
            {
                route = new ConnectionRouteCache();
                _connectionRoutes.Add(target, route);
            }

            float sourceTrim = geometry.straight ? 0f : geometry.sourceTrimProgress;
            float targetTrim = Mathf.Max(
                GetIncomingTrimProgress(target),
                GetStableConnectionRouteTargetProgress(target));
            if (!geometry.straight)
                targetTrim = Mathf.Max(targetTrim, geometry.targetTrimProgress);
            BuildConnectionPath(geometry, sourceTrim, targetTrim);

            route.points.Clear();
            route.distances.Clear();
            route.length = 0f;
            for (int i = 0; i < _connectionPath.Count; i++)
            {
                Vector3 point = _connectionPath[i];
                if (route.points.Count > 0)
                    route.length += Vector2.Distance(route.points[route.points.Count - 1], point);
                route.points.Add(point);
                route.distances.Add(route.length);
            }

            route.sourceStartProgress = 1f - Mathf.Clamp(sourceTrim, 0f, connectionMaxProgress);
            route.targetEndProgress = Mathf.Clamp01(targetTrim);
            route.version = _connectionRouteVersion;
            return route.points.Count >= 2 && route.length > 1e-5f;
        }

        void AppendConnectionPath(ConnectionGeometry geometry,
            float sharedSourceTrimProgress, float sharedTargetTrimProgress, float sourceWidth)
        {
            BuildConnectionPath(geometry, sharedSourceTrimProgress, sharedTargetTrimProgress);
            if (_connectionPath.Count < 2) return;

            float sourceHalfWidth = Conveyor.HalfWidth;
            float targetHalfWidth = geometry.target.HalfWidth;
            float sourceWallWidth = showWalls ? Mathf.Abs(wallWidth) : 0f;
            float targetWallWidth = geometry.targetRenderer.showWalls
                ? Mathf.Abs(geometry.targetRenderer.wallWidth)
                : 0f;
            float beltZ = Mathf.Min(zOffset, geometry.targetRenderer.zOffset) + JunctionZOffset;
            float wallZ = Mathf.Min(zOffset + wallZOffset,
                geometry.targetRenderer.zOffset + geometry.targetRenderer.wallZOffset) +
                JunctionZOffset - 0.001f;
            float startV = (1f - sharedSourceTrimProgress) * _vTiles;

            AppendConnectionStrip(_connectionPath, sourceHalfWidth, targetHalfWidth,
                sourceWallWidth, targetWallWidth, 0, beltZ, sourceWidth, startV, _triBelt);

            if (showWalls)
            {
                AppendConnectionStrip(_connectionPath, sourceHalfWidth, targetHalfWidth,
                    sourceWallWidth, targetWallWidth, 1, wallZ, sourceWidth, startV, _triOuter);
                AppendConnectionStrip(_connectionPath, sourceHalfWidth, targetHalfWidth,
                    sourceWallWidth, targetWallWidth, 2, wallZ, sourceWidth, startV, _triInner);
            }
        }

        void BuildConnectionPath(ConnectionGeometry geometry,
            float sharedSourceTrimProgress, float sharedTargetTrimProgress)
        {
            _connectionPath.Clear();

            Vector3 pathStart = Conveyor.GetPositionOnSpline(
                1f - Mathf.Clamp(sharedSourceTrimProgress, 0f, connectionMaxProgress), 0f);
            float targetEndProgress = Mathf.Clamp01(sharedTargetTrimProgress);

            AddConnectionPoint(pathStart);

            if (geometry.straight)
            {
                AddConnectionPoint(geometry.sourceEnd);
                AddConnectionPoint(geometry.targetStart);
                AppendTargetLeadPoints(geometry.target, 0f, targetEndProgress);
            }
            else if (geometry.exactFillet)
            {
                AddConnectionPoint(geometry.sourceTangentPoint);

                Vector3 radialStart = geometry.sourceTangentPoint - geometry.arcCenter;
                float startAngle = Mathf.Atan2(radialStart.y, radialStart.x);
                int arcSegments = Mathf.Max(4,
                    Mathf.CeilToInt(connectionSegments * geometry.turnAngle / (Mathf.PI * 0.5f)));

                for (int i = 1; i <= arcSegments; i++)
                {
                    float f = i / (float)arcSegments;
                    float angle = startAngle + geometry.turnSign * geometry.turnAngle * f;
                    float radius = radialStart.magnitude;
                    Vector3 point = geometry.arcCenter +
                                    new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                    AddConnectionPoint(point);
                }

                // Dùng điểm thật trên target thay vì chỉ dựa vào control point. Đây là phần
                // "mượn" lead-in của conveyor được kết nối để đường render liên tục.
                AddConnectionPoint(geometry.targetTangentPoint);
                AppendTargetLeadPoints(geometry.target, geometry.targetTrimProgress,
                    targetEndProgress);
            }
            else
            {
                // Endpoint không snap: blend Hermite giữa hai điểm trim, không chèn disk.
                Vector3 start = geometry.sourceTangentPoint;
                Vector3 end = geometry.targetTangentPoint;
                AddConnectionPoint(start);
                float distance = Vector2.Distance(start, end);
                float handle = distance * 0.45f;
                Vector3 controlA = start + geometry.sourceDirection * handle;
                Vector3 controlB = end - geometry.targetDirection * handle;
                int count = Mathf.Max(4, connectionSegments);
                for (int i = 1; i <= count; i++)
                {
                    float t = i / (float)count;
                    AddConnectionPoint(EvaluateCubicBezier(start, controlA, controlB, end, t));
                }
                AppendTargetLeadPoints(geometry.target, geometry.targetTrimProgress,
                    targetEndProgress);
            }

        }

        void AppendTargetLeadPoints(ConveyorSpline target, float startProgress, float endProgress)
        {
            if (target == null) return;

            startProgress = Mathf.Clamp01(startProgress);
            endProgress = Mathf.Clamp01(endProgress);
            if (endProgress <= startProgress + 1e-5f) return;

            int sampleCount = Mathf.Clamp(
                Mathf.CeilToInt(Mathf.Max(4, connectionSegments) * (endProgress - startProgress)),
                2,
                Mathf.Max(2, connectionSegments));
            for (int i = 1; i <= sampleCount; i++)
            {
                float progress = Mathf.Lerp(startProgress, endProgress, i / (float)sampleCount);
                AddConnectionPoint(target.GetPositionOnSpline(progress, 0f));
            }
        }

        void AddConnectionPoint(Vector3 point)
        {
            if (_connectionPath.Count == 0 ||
                (_connectionPath[_connectionPath.Count - 1] - point).sqrMagnitude > 1e-8f)
                _connectionPath.Add(point);
        }

        // stripType: 0 = belt, 1 = outer rail (+normal), 2 = inner rail (-normal).
        void AppendConnectionStrip(List<Vector3> centers,
            float sourceHalfWidth, float targetHalfWidth,
            float sourceWallWidth, float targetWallWidth,
            int stripType, float zoff, float sourceWidth, float startV, List<int> tris)
        {
            int count = centers.Count;
            if (count < 2) return;

            _connectionDistances.Clear();
            _connectionDistances.Add(0f);
            float totalLength = 0f;
            for (int i = 1; i < count; i++)
            {
                totalLength += Vector2.Distance(centers[i - 1], centers[i]);
                _connectionDistances.Add(totalLength);
            }
            if (totalLength <= 1e-5f) return;

            int baseIndex = _verts.Count;
            for (int i = 0; i < count; i++)
            {
                Vector3 tangent;
                if (i == 0) tangent = centers[1] - centers[0];
                else if (i == count - 1) tangent = centers[count - 1] - centers[count - 2];
                else tangent = centers[i + 1] - centers[i - 1];
                tangent.z = 0f;
                if (tangent.sqrMagnitude < 1e-6f) tangent = geometrySafeDirection(centers, i);
                tangent.Normalize();
                Vector3 normal = new Vector3(-tangent.y, tangent.x, 0f);

                float blend = _connectionDistances[i] / totalLength;
                float halfWidth = Mathf.Lerp(sourceHalfWidth, targetHalfWidth, blend);
                float currentWallWidth = Mathf.Lerp(sourceWallWidth, targetWallWidth, blend);

                float offsetA;
                float offsetB;
                float uA;
                float uB;
                if (stripType == 0)
                {
                    offsetA = halfWidth;
                    offsetB = -halfWidth;
                    uA = tilesAcrossWidth;
                    uB = 0f;
                }
                else if (stripType == 1)
                {
                    offsetA = halfWidth + currentWallWidth;
                    offsetB = halfWidth;
                    uA = currentWallWidth * tilesAcrossWidth / Mathf.Max(1e-4f, sourceWidth);
                    uB = 0f;
                }
                else
                {
                    offsetA = -halfWidth;
                    offsetB = -halfWidth - currentWallWidth;
                    uA = 0f;
                    uB = currentWallWidth * tilesAcrossWidth / Mathf.Max(1e-4f, sourceWidth);
                }

                float curvatureClearance = Mathf.Max(0.01f, sourceWidth * 0.03f);
                offsetA = ClampOffsetAgainstPathCurvature(
                    centers, i, normal, offsetA, curvatureClearance);
                offsetB = ClampOffsetAgainstPathCurvature(
                    centers, i, normal, offsetB, curvatureClearance);

                Vector3 worldA = centers[i] + normal * offsetA;
                Vector3 worldB = centers[i] + normal * offsetB;
                worldA.z += zoff;
                worldB.z += zoff;
                _verts.Add(transform.InverseTransformPoint(worldA));
                _verts.Add(transform.InverseTransformPoint(worldB));

                float v = startV + _connectionDistances[i] * tilesAcrossWidth /
                    Mathf.Max(1e-4f, sourceWidth);
                _uvs.Add(new Vector2(uA, v));
                _uvs.Add(new Vector2(uB, v));
            }

            for (int i = 0; i < count - 1; i++)
            {
                int vi = baseIndex + i * 2;
                AppendStripTriangle(tris, vi, vi + 2, vi + 1);
                AppendStripTriangle(tris, vi + 1, vi + 2, vi + 3);
            }
        }

        static float ClampOffsetAgainstPathCurvature(List<Vector3> centers, int index,
            Vector3 normal, float offset, float clearance)
        {
            if (centers == null || centers.Count < 3 || index < 0 || index >= centers.Count)
                return offset;

            int previous = index > 0 ? index - 1 : 0;
            int next = index < centers.Count - 1 ? index + 1 : centers.Count - 1;
            if (previous == index || next == index) return offset;

            Vector2 a = centers[previous];
            Vector2 b = centers[index];
            Vector2 c = centers[next];
            return ClampOffsetAgainstCurvature(a, b, c, normal, offset, clearance);
        }

        static float ClampOffsetAgainstCurvature(Vector2 a, Vector2 b, Vector2 c,
            Vector3 normal, float offset, float clearance)
        {
            float determinant = 2f * (a.x * (b.y - c.y) +
                                      b.x * (c.y - a.y) +
                                      c.x * (a.y - b.y));
            if (Mathf.Abs(determinant) <= 1e-6f) return offset;

            float aSq = a.sqrMagnitude;
            float bSq = b.sqrMagnitude;
            float cSq = c.sqrMagnitude;
            Vector2 center = new Vector2(
                (aSq * (b.y - c.y) + bSq * (c.y - a.y) + cSq * (a.y - b.y)) /
                determinant,
                (aSq * (c.x - b.x) + bSq * (a.x - c.x) + cSq * (b.x - a.x)) /
                determinant);

            Vector2 radial = b - center;
            float radius = radial.magnitude;
            if (radius <= 1e-5f) return 0f;

            float alignment = Vector2.Dot((Vector2)normal, radial / radius);
            if (Mathf.Abs(alignment) < 0.75f || offset * alignment >= 0f)
                return offset;

            float maxInwardOffset = Mathf.Max(0f, radius - Mathf.Max(0f, clearance)) /
                                    Mathf.Abs(alignment);
            return Mathf.Sign(offset) * Mathf.Min(Mathf.Abs(offset), maxInwardOffset);
        }

        static Vector3 geometrySafeDirection(List<Vector3> centers, int index)
        {
            for (int offset = 1; offset < centers.Count; offset++)
            {
                int next = Mathf.Min(centers.Count - 1, index + offset);
                Vector3 direction = centers[next] - centers[index];
                direction.z = 0f;
                if (direction.sqrMagnitude > 1e-6f) return direction;

                int previous = Mathf.Max(0, index - offset);
                direction = centers[index] - centers[previous];
                direction.z = 0f;
                if (direction.sqrMagnitude > 1e-6f) return direction;
            }
            return Vector3.right;
        }

        static Vector3 EvaluateCubicBezier(Vector3 start, Vector3 controlA, Vector3 controlB,
            Vector3 end, float t)
        {
            float inverseT = 1f - t;
            return inverseT * inverseT * inverseT * start +
                   3f * inverseT * inverseT * t * controlA +
                   3f * inverseT * t * t * controlB +
                   t * t * t * end;
        }

        void AppendMultiIncomingJunction(float sourceWidth)
        {
            if (Conveyor.IsClosed) return;

            Vector3 center = Conveyor.GetPositionOnSpline(0f, 0f);
            ConveyorConnections[] connections = Object.FindObjectsByType<ConveyorConnections>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var sources = new List<ConveyorSpline>();
            float maxHalfWidth = Conveyor.HalfWidth;
            float maxWallWidth = Mathf.Abs(wallWidth);
            float frontZOffset = zOffset + Mathf.Min(0f, wallZOffset) + JunctionZOffset;

            foreach (ConveyorConnections connection in connections)
            {
                if (connection == null || connection.next == null || !connection.next.Contains(Conveyor)) continue;

                ConveyorSpline source = connection.GetComponent<ConveyorSpline>();
                if (source == null || source == Conveyor || source.IsClosed || sources.Contains(source)) continue;

                Vector3 sourceEnd = source.GetPositionOnSpline(1f, 0f);
                float snapTolerance = Mathf.Max(0.05f, Mathf.Max(sourceWidth, source.beltWidth) * 0.1f);
                if (Vector2.Distance(center, sourceEnd) > snapTolerance) continue;

                sources.Add(source);
                maxHalfWidth = Mathf.Max(maxHalfWidth, source.HalfWidth);

                ConveyorBeltRenderer sourceRenderer = source.GetComponent<ConveyorBeltRenderer>();
                if (sourceRenderer != null && sourceRenderer.showWalls)
                {
                    maxWallWidth = Mathf.Max(maxWallWidth, Mathf.Abs(sourceRenderer.wallWidth));
                    float sourceFront = sourceRenderer.zOffset + Mathf.Min(0f, sourceRenderer.wallZOffset);
                    frontZOffset = Mathf.Min(frontZOffset, sourceFront + JunctionZOffset);
                }
            }

            if (sources.Count < 2) return;
            AppendRuleTile(center, Conveyor.GetTangent(0f), sources, false, sourceWidth,
                maxHalfWidth, maxWallWidth, frontZOffset);
        }

        void AppendRuleTile(Vector3 center, Vector3 primaryDirection, List<ConveyorSpline> linkedArms,
            bool linkedArmsStartAtZero, float sourceWidth, float maxHalfWidth, float maxWallWidth,
            float frontZOffset)
        {
            Vector2 primary = primaryDirection;
            if (primary.sqrMagnitude < 1e-6f) return;
            primary.Normalize();

            // Only render rule tiles for orthogonal layouts: straight, turn, T and cross rules.
            Vector2 axisX = primary;
            Vector2 axisY = new Vector2(-axisX.y, axisX.x);
            bool[] openSides = new bool[4];
            if (!MarkRuleTileSide(primary, axisX, axisY, openSides)) return;

            foreach (ConveyorSpline linkedArm in linkedArms)
            {
                Vector3 armDirection = linkedArmsStartAtZero
                    ? linkedArm.GetTangent(0f)
                    : -linkedArm.GetTangent(1f);
                if (!MarkRuleTileSide(armDirection, axisX, axisY, openSides)) return;
            }

            int openSideCount = 0;
            foreach (bool open in openSides)
                if (open) openSideCount++;
            if (openSideCount < 3) return;

            float halfExtent = Mathf.Max(maxHalfWidth + maxWallWidth * 0.5f, sourceWidth * 0.7f);
            float uvScale = tilesAcrossWidth / Mathf.Max(1e-4f, sourceWidth);
            AppendRuleTileBelt(center, axisX, axisY, halfExtent, frontZOffset, uvScale);

            float tileWallWidth = Mathf.Min(maxWallWidth, halfExtent * 0.5f);
            if (tileWallWidth <= 1e-4f) return;

            Vector2[] sideDirections = { axisX, axisY, -axisX, -axisY };
            for (int side = 0; side < sideDirections.Length; side++)
            {
                if (!openSides[side])
                {
                    // This is the exposed outer border of the rule tile, so it intentionally uses
                    // the outer-wall material/submesh even when the two rail materials differ.
                    AppendRuleTileWall(center, sideDirections[side], halfExtent, tileWallWidth,
                        frontZOffset - 0.001f, uvScale);
                }
            }
        }

        static bool MarkRuleTileSide(Vector3 direction3, Vector2 axisX, Vector2 axisY, bool[] openSides)
        {
            Vector2 direction = direction3;
            if (direction.sqrMagnitude < 1e-6f) return false;
            direction.Normalize();

            float dotX = Vector2.Dot(direction, axisX);
            float dotY = Vector2.Dot(direction, axisY);
            float absX = Mathf.Abs(dotX);
            float absY = Mathf.Abs(dotY);
            if (Mathf.Max(absX, absY) < RuleTileDirectionDotThreshold) return false;

            int side = absX > absY ? (dotX >= 0f ? 0 : 2) : (dotY >= 0f ? 1 : 3);
            openSides[side] = true;
            return true;
        }

        void AppendRuleTileBelt(Vector3 center, Vector2 axisX, Vector2 axisY, float halfExtent,
            float zoff, float uvScale)
        {
            int vi = _verts.Count;
            float tileUvSize = halfExtent * 2f * uvScale;
            AddJunctionVertex(center - (Vector3)(axisX + axisY) * halfExtent, zoff, Vector2.zero);
            AddJunctionVertex(center + (Vector3)(axisX - axisY) * halfExtent, zoff,
                new Vector2(tileUvSize, 0f));
            AddJunctionVertex(center + (Vector3)(axisX + axisY) * halfExtent, zoff,
                new Vector2(tileUvSize, tileUvSize));
            AddJunctionVertex(center + (Vector3)(-axisX + axisY) * halfExtent, zoff,
                new Vector2(0f, tileUvSize));

            _triBelt.Add(vi);
            _triBelt.Add(vi + 2);
            _triBelt.Add(vi + 1);
            _triBelt.Add(vi);
            _triBelt.Add(vi + 3);
            _triBelt.Add(vi + 2);
        }

        void AppendRuleTileWall(Vector3 center, Vector2 side, float halfExtent, float wallWidth,
            float zoff, float uvScale)
        {
            int vi = _verts.Count;
            Vector2 edge = new Vector2(-side.y, side.x);
            Vector3 innerCenter = center + (Vector3)side * halfExtent;
            Vector3 outerCenter = center + (Vector3)side * (halfExtent + wallWidth);
            float tileUvSize = halfExtent * 2f * uvScale;
            float wallUvWidth = wallWidth * uvScale;
            AddJunctionVertex(innerCenter - (Vector3)edge * halfExtent, zoff, new Vector2(0f, 0f));
            AddJunctionVertex(innerCenter + (Vector3)edge * halfExtent, zoff, new Vector2(0f, tileUvSize));
            AddJunctionVertex(outerCenter + (Vector3)edge * halfExtent, zoff,
                new Vector2(wallUvWidth, tileUvSize));
            AddJunctionVertex(outerCenter - (Vector3)edge * halfExtent, zoff,
                new Vector2(wallUvWidth, 0f));

            _triOuter.Add(vi);
            _triOuter.Add(vi + 1);
            _triOuter.Add(vi + 2);
            _triOuter.Add(vi);
            _triOuter.Add(vi + 2);
            _triOuter.Add(vi + 3);
        }

        void AddJunctionVertex(Vector3 world, float zoff, Vector2 uv)
        {
            world.z += zoff;
            _verts.Add(transform.InverseTransformPoint(world));
            _uvs.Add(uv);
        }

        void Update()
        {
            // Có chỉnh sửa từ Inspector lúc play -> dựng lại mesh và xả material instances cũ
            // (Update sẽ tự tạo lại instances cuộn từ material mới ở khối bên dưới).
            if (_rebuildQueued)
            {
                _rebuildQueued = false;
                RebuildMeshAndMaterials();
            }

            if (_mr == null) return;

            if (Application.isPlaying)
            {
                // Dùng material instances để không ghi đè asset gốc.
                if (_scrollMats == null) _scrollMats = _mr.materials; // .materials tạo instances cho mọi submesh
                _scroll += scrollSpeed * Time.deltaTime;

                // offset âm theo V -> hoa văn chạy về phía t=1 khi scrollSpeed dương.
                // Băng (index 0) luôn cuộn; thành (1,2) chỉ cuộn khi scrollWalls.
                for (int i = 0; i < _scrollMats.Length; i++)
                {
                    if (_scrollMats[i] == null) continue;
                    if (i > 0 && !scrollWalls) continue;
                    Vector2 off = _scrollMats[i].mainTextureOffset;
                    off.y = -_scroll;
                    _scrollMats[i].mainTextureOffset = off;
                }
            }
        }
    }
}
