using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace FruitSort
{
    /// <summary>
    /// Nguồn dữ liệu hình học duy nhất của conveyor.
    /// Spline được bake thành một LUT world-space để movement và renderer cùng sample.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SplineContainer))]
    public sealed class ConveyorSpline : MonoBehaviour
    {
        [Min(0.01f)] public float beltWidth = 3f;
        [Min(8)] public int bakeResolution = 96;

        [Header("Straight path / rounded corners")]
        public bool straightEdges = true;
        [Min(0f)] public float cornerRadius = 0.5f;
        [Min(1)] public int cornerSegments = 8;

        [Tooltip("Chỉ bật khi transform conveyor thực sự thay đổi trong lúc chạy.")]
        public bool autoRebakeAtRuntime;

        [Tooltip("Bật với conveyor gameplay thật. Tắt với spline chỉ dùng để render tạm, " +
                 "ví dụ renderer hợp nhất của ConveyorSwitch.")]
        public bool registerWithFallingPixelManager = true;

        readonly List<Vector3> _polyline = new List<Vector3>(128);
        Vector3[] _positions;
        Vector3[] _tangents;
        SplineContainer _container;
        float _length;
        int _resolution;
        bool _isBaked;
        bool _rebuildQueued;
        bool _registeredWithFallingPixelManager;

        public SplineContainer Container =>
            _container != null ? _container : (_container = GetComponent<SplineContainer>());

        public float HalfWidth => beltWidth * 0.5f;
        public bool IsClosed => HasValidSpline && Container.Spline.Closed;
        public float Length
        {
            get
            {
                EnsureBaked();
                return _length;
            }
        }

        bool HasValidSpline =>
            Container != null && Container.Spline != null && Container.Spline.Count >= 2;

        void OnEnable()
        {
            Bake();

            if (Application.isPlaying && registerWithFallingPixelManager &&
                FallingPixelManager.Instance != null)
            {
                FallingPixelManager.Instance.RegisterConveyor(this);
                _registeredWithFallingPixelManager = true;
            }
        }

        void OnDisable()
        {
            if (!_registeredWithFallingPixelManager)
                return;

            if (FallingPixelManager.Instance != null)
                FallingPixelManager.Instance.UnregisterConveyor(this);

            _registeredWithFallingPixelManager = false;
        }

        void OnValidate()
        {
            beltWidth = Mathf.Max(0.01f, beltWidth);
            bakeResolution = Mathf.Max(8, bakeResolution);
            cornerRadius = Mathf.Max(0f, cornerRadius);
            cornerSegments = Mathf.Max(1, cornerSegments);

            if (Application.isPlaying)
            {
                _rebuildQueued = true;
                return;
            }

            Bake();
            RebuildRenderer();
        }

        void Update()
        {
            if (_rebuildQueued)
            {
                _rebuildQueued = false;
                Bake();
                RebuildRenderer();
            }

            if (Application.isPlaying && !autoRebakeAtRuntime)
                return;

            if (!transform.hasChanged)
                return;

            transform.hasChanged = false;
            Bake();
            RebuildRenderer();
        }

        void RebuildRenderer()
        {
            ConveyorConnections.RebuildNetwork(this);
        }

        [ContextMenu("Bake Conveyor")]
        public void Bake()
        {
            _isBaked = false;
            _length = 0f;

            if (!HasValidSpline)
                return;

            int resolution = Mathf.Max(8, bakeResolution);
            EnsureArrays(resolution);

            bool success = straightEdges
                ? BuildRoundedPolyline(_polyline) && BakePolyline(_polyline, resolution)
                : BakeUnitySpline(resolution);

            _isBaked = success;
        }

        void EnsureArrays(int resolution)
        {
            if (_positions != null && _resolution == resolution)
                return;

            _resolution = resolution;
            _positions = new Vector3[resolution + 1];
            _tangents = new Vector3[resolution + 1];
        }

        bool BakeUnitySpline(int resolution)
        {
            for (int i = 0; i <= resolution; i++)
            {
                float t = i / (float)resolution;
                Container.Evaluate(t, out float3 position, out float3 tangent, out _);
                _positions[i] = (Vector3)position;
                _tangents[i] = Normalize2D((Vector3)tangent);
            }

            if (IsClosed && resolution >= 2)
            {
                _positions[resolution] = _positions[0];
                Vector3 seamTangent = Normalize2D(_positions[1] - _positions[resolution - 1]);
                _tangents[0] = seamTangent;
                _tangents[resolution] = seamTangent;
            }

            _length = CalculateLength(_positions);
            return _length > 0.0001f;
        }

        bool BuildRoundedPolyline(List<Vector3> output)
        {
            output.Clear();

            Spline spline = Container.Spline;
            int count = spline.Count;
            bool closed = spline.Closed;

            for (int index = 0; index < count; index++)
            {
                Vector3 current = transform.TransformPoint((Vector3)spline[index].Position);
                bool endpoint = !closed && (index == 0 || index == count - 1);

                if (endpoint || cornerRadius <= 0.0001f || count < 3)
                {
                    AddUnique(output, current);
                    continue;
                }

                int previousIndex = (index - 1 + count) % count;
                int nextIndex = (index + 1) % count;
                Vector3 previous = transform.TransformPoint((Vector3)spline[previousIndex].Position);
                Vector3 next = transform.TransformPoint((Vector3)spline[nextIndex].Position);

                AddRoundedCorner(output, previous, current, next);
            }

            if (closed && output.Count > 1)
                AddUnique(output, output[0]);

            return output.Count >= 2;
        }

        void AddRoundedCorner(List<Vector3> output, Vector3 previous, Vector3 current, Vector3 next)
        {
            Vector3 incoming = current - previous;
            Vector3 outgoing = next - current;
            float incomingLength = incoming.magnitude;
            float outgoingLength = outgoing.magnitude;

            if (incomingLength < 0.0001f || outgoingLength < 0.0001f)
            {
                AddUnique(output, current);
                return;
            }

            incoming /= incomingLength;
            outgoing /= outgoingLength;

            float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(-incoming, outgoing), -1f, 1f));
            if (angle < 0.001f || Mathf.PI - angle < 0.001f)
            {
                AddUnique(output, current);
                return;
            }

            float halfAngle = angle * 0.5f;
            float tangentDistance = cornerRadius / Mathf.Tan(halfAngle);
            tangentDistance = Mathf.Min(tangentDistance, Mathf.Min(incomingLength, outgoingLength) * 0.5f);

            float radius = tangentDistance * Mathf.Tan(halfAngle);
            Vector3 entry = current - incoming * tangentDistance;
            Vector3 exit = current + outgoing * tangentDistance;
            Vector3 bisector = (-incoming + outgoing).normalized;
            Vector3 center = current + bisector * (radius / Mathf.Sin(halfAngle));

            Vector3 startRadius = entry - center;
            Vector3 endRadius = exit - center;
            int segments = Mathf.Max(1, cornerSegments);

            for (int i = 0; i <= segments; i++)
                AddUnique(output, center + Vector3.Slerp(startRadius, endRadius, i / (float)segments));
        }

        bool BakePolyline(List<Vector3> path, int resolution)
        {
            int count = path.Count;
            if (count < 2)
                return false;

            float[] cumulative = new float[count];
            for (int i = 1; i < count; i++)
                cumulative[i] = cumulative[i - 1] + Vector2.Distance(path[i - 1], path[i]);

            _length = cumulative[count - 1];
            if (_length <= 0.0001f)
                return false;

            int segment = 0;
            for (int i = 0; i <= resolution; i++)
            {
                float targetDistance = _length * i / resolution;
                while (segment < count - 2 && cumulative[segment + 1] < targetDistance)
                    segment++;

                float segmentStart = cumulative[segment];
                float segmentEnd = cumulative[segment + 1];
                float blend = Mathf.InverseLerp(segmentStart, segmentEnd, targetDistance);
                Vector3 a = path[segment];
                Vector3 b = path[segment + 1];

                _positions[i] = Vector3.LerpUnclamped(a, b, blend);
                _tangents[i] = Normalize2D(b - a);
            }

            SmoothTangents();
            return true;
        }

        void SmoothTangents()
        {
            int first = 0;
            int last = _resolution;
            if (IsClosed && _resolution >= 2)
            {
                Vector3 seamTangent = Normalize2D(_positions[1] - _positions[_resolution - 1]);
                _tangents[first] = seamTangent;
                _tangents[last] = seamTangent;
                first++;
                last--;
            }

            for (int i = first; i <= last; i++)
            {
                Vector3 tangent;
                if (i == 0)
                    tangent = _positions[1] - _positions[0];
                else if (i == _resolution)
                    tangent = _positions[_resolution] - _positions[_resolution - 1];
                else
                    tangent = _positions[i + 1] - _positions[i - 1];

                _tangents[i] = Normalize2D(tangent);
            }
        }

        static float CalculateLength(Vector3[] positions)
        {
            float length = 0f;
            for (int i = 1; i < positions.Length; i++)
                length += Vector2.Distance(positions[i - 1], positions[i]);
            return length;
        }

        static Vector3 Normalize2D(Vector3 value)
        {
            value.z = 0f;
            return value.sqrMagnitude > 0.000001f ? value.normalized : Vector3.right;
        }

        static void AddUnique(List<Vector3> points, Vector3 point)
        {
            if (points.Count == 0 || (points[points.Count - 1] - point).sqrMagnitude > 0.00000001f)
                points.Add(point);
        }

        void EnsureBaked()
        {
            if (!_isBaked)
                Bake();
        }

        public bool TrySampleCenterline(float progress, out Vector3 position, out Vector3 tangent)
        {
            EnsureBaked();

            if (!_isBaked || _positions == null)
            {
                position = transform.position;
                tangent = Vector3.right;
                return false;
            }

            float sample = Mathf.Clamp01(progress) * _resolution;
            int index = Mathf.Min(Mathf.FloorToInt(sample), _resolution - 1);
            float blend = sample - index;

            position = Vector3.LerpUnclamped(_positions[index], _positions[index + 1], blend);
            tangent = Normalize2D(Vector3.Lerp(_tangents[index], _tangents[index + 1], blend));
            return true;
        }

        public Vector3 GetPositionOnSpline(float progress, float lateralOffset)
        {
            if (!TrySampleCenterline(progress, out Vector3 center, out Vector3 tangent))
                return transform.position;

            Vector3 normal = new Vector3(-tangent.y, tangent.x, 0f);
            return center + normal * lateralOffset;
        }

        public Vector3 GetTangent(float progress)
        {
            return TrySampleCenterline(progress, out _, out Vector3 tangent)
                ? tangent
                : Vector3.right;
        }

        public float FindClosestProgress(Vector3 worldPosition, out float closestDistance)
        {
            EnsureBaked();

            if (!_isBaked || _positions == null)
            {
                closestDistance = float.MaxValue;
                return 0f;
            }

            float bestDistanceSquared = float.MaxValue;
            float bestProgress = 0f;
            Vector2 point = worldPosition;

            for (int i = 0; i < _resolution; i++)
            {
                Vector2 a = _positions[i];
                Vector2 b = _positions[i + 1];
                Vector2 ab = b - a;
                float denominator = ab.sqrMagnitude;
                float blend = denominator > 0.000001f
                    ? Mathf.Clamp01(Vector2.Dot(point - a, ab) / denominator)
                    : 0f;

                Vector2 closest = a + ab * blend;
                float distanceSquared = (point - closest).sqrMagnitude;
                if (distanceSquared >= bestDistanceSquared)
                    continue;

                bestDistanceSquared = distanceSquared;
                bestProgress = (i + blend) / _resolution;
            }

            closestDistance = Mathf.Sqrt(bestDistanceSquared);
            return bestProgress;
        }

        public float GetProgressThroughKnot(int knotIndex)
        {
            if (!HasValidSpline)
                return 0f;

            knotIndex = Mathf.Clamp(knotIndex, 0, Container.Spline.Count - 1);
            Vector3 knot = transform.TransformPoint((Vector3)Container.Spline[knotIndex].Position);
            return FindClosestProgress(knot, out _);
        }

        public float GetEndpointStraightLead(bool atStart)
        {
            if (!HasValidSpline)
                return 0f;

            int first = atStart ? 0 : Container.Spline.Count - 1;
            int second = atStart ? 1 : Container.Spline.Count - 2;
            Vector3 a = transform.TransformPoint((Vector3)Container.Spline[first].Position);
            Vector3 b = transform.TransformPoint((Vector3)Container.Spline[second].Position);
            return Vector2.Distance(a, b);
        }

        public float GetSplineLength() => Length;

        void OnDrawGizmosSelected()
        {
            EnsureBaked();
            if (!_isBaked)
                return;

            Gizmos.color = Color.cyan;
            Vector3 previousLeft = GetPositionOnSpline(0f, HalfWidth);
            Vector3 previousRight = GetPositionOnSpline(0f, -HalfWidth);

            for (int i = 1; i <= 64; i++)
            {
                float progress = i / 64f;
                Vector3 left = GetPositionOnSpline(progress, HalfWidth);
                Vector3 right = GetPositionOnSpline(progress, -HalfWidth);
                Gizmos.DrawLine(previousLeft, left);
                Gizmos.DrawLine(previousRight, right);
                previousLeft = left;
                previousRight = right;
            }
        }
    }
}
