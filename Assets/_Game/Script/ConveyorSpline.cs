using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace FruitSort
{
    /// <summary>
    /// Bọc một SplineContainer làm "băng chuyền".
    /// Cung cấp vị trí trên spline theo progress (0..1) + lệch ngang trong bề rộng băng chuyền.
    ///
    /// TỐI ƯU: spline được "bake" 1 lần thành LOOKUP TABLE (LUT) gồm vị trí tâm + tiếp tuyến
    /// đã chuẩn hoá. Mọi truy vấn runtime chỉ là LERP vào mảng -> rất rẻ, thay cho
    /// Container.Evaluate() (đắt) bị gọi tới 2 lần/dot/frame. Chiều dài spline cũng được
    /// cache (không CalculateLength mỗi frame). Tự re-bake khi transform của băng chuyền đổi.
    ///
    /// HÌNH DẠNG: mặc định (straightEdges) dựng centerline bằng các ĐOẠN THẲNG nối các knot,
    /// CHỈ bo cong (fillet) tại các góc với bán kính cornerRadius. Tắt straightEdges để dùng
    /// đường cong Bezier gốc của spline.
    /// </summary>
    [RequireComponent(typeof(SplineContainer))]
    public class ConveyorSpline : MonoBehaviour
    {
        [Tooltip("Bề rộng băng chuyền (world unit). Dùng để random vị trí ban đầu và clamp lệch ngang.")]
        public float beltWidth = 3f;

        [Tooltip("Số điểm sample khi bake LUT. Cao hơn = mượt hơn nhưng tốn RAM/tải bake. 64-128 là đủ cho hầu hết băng chuyền.")]
        [Min(2)] public int bakeResolution = 96;

        [Header("Cạnh thẳng / Bo góc")]
        [Tooltip("BẬT (mặc định): cạnh băng chuyền THẲNG giữa các knot, chỉ bo cong ở góc. " +
                 "TẮT: dùng đường cong Bezier gốc của spline (cong khắp nơi).")]
        public bool straightEdges = true;

        [Tooltip("Bán kính bo tròn ở mỗi góc (world unit). 0 = góc nhọn (gấp khúc). " +
                 "Tự clamp lại nếu lớn hơn nửa đoạn thẳng kề bên.")]
        [Min(0f)] public float cornerRadius = 0.5f;

        [Tooltip("Số đoạn chia cho mỗi cung bo góc. Cao hơn = góc mượt hơn.")]
        [Min(1)] public int cornerSegments = 8;

        [Tooltip("Lúc PLAY có tự bake lại khi transform băng chuyền đổi không? " +
                 "TẮT (mặc định) = chỉ bake 1 lần -> tránh re-bake 256 evaluate mỗi frame nếu hasChanged bị bật. " +
                 "Bật nếu băng chuyền THỰC SỰ di chuyển lúc chơi.")]
        public bool autoRebakeAtRuntime = false;

        SplineContainer _container;
        public SplineContainer Container
        {
            get { if (_container == null) _container = GetComponent<SplineContainer>(); return _container; }
        }

        public float HalfWidth => beltWidth * 0.5f;

        bool HasSpline => Container != null && Container.Spline != null && Container.Spline.Count > 1;

        // ---- LUT đã bake (world space) ----
        Vector3[] _lutPos;   // vị trí TÂM spline
        Vector3[] _lutTan;   // tiếp tuyến đã chuẩn hoá (XY)
        float _bakedLength = 1f;
        bool _baked = false;
        int _bakedRes = -1;

        // Buffer tái dùng khi dựng polyline bo góc (tránh alloc mỗi lần bake).
        readonly List<Vector3> _pathPts = new List<Vector3>(256);

        void Awake() { Bake(); }

        void OnEnable() { Bake(); }

        void Update()
        {
            // Lúc PLAY: mặc định KHÔNG auto re-bake để tránh 256 evaluate/frame nếu hasChanged
            // bị bật liên tục. Băng chuyền tĩnh -> bake 1 lần ở Awake/OnEnable là đủ.
            if (Application.isPlaying && !autoRebakeAtRuntime) return;

            // Editor (chỉnh spline) hoặc khi bật autoRebakeAtRuntime: re-bake khi transform đổi.
            if (transform.hasChanged)
            {
                Bake();
                transform.hasChanged = false;
            }
        }

        void EnsureLutArrays(int res)
        {
            if (_lutPos == null || _bakedRes != res)
            {
                _lutPos = new Vector3[res + 1];
                _lutTan = new Vector3[res + 1];
                _bakedRes = res;
            }
        }

        /// <summary>Bake (hoặc re-bake) LUT từ spline hiện tại. Có thể gọi tay nếu sửa spline lúc runtime.</summary>
        public void Bake()
        {
            if (!HasSpline) { _baked = false; return; }

            if (straightEdges && BakeStraight()) { _baked = true; return; }

            BakeFromSpline();
            _baked = true;
        }

        /// <summary>Bake theo đường cong Bezier gốc của spline (cong khắp nơi).</summary>
        void BakeFromSpline()
        {
            int res = Mathf.Max(2, bakeResolution);
            EnsureLutArrays(res);

            for (int i = 0; i <= res; i++)
            {
                float t = i / (float)res;
                Container.Evaluate(t, out float3 pos, out float3 tan, out _);

                Vector3 wt = (Vector3)tan; wt.z = 0f;
                if (wt.sqrMagnitude < 1e-6f) wt = Vector3.right; else wt.Normalize();

                _lutPos[i] = (Vector3)pos;
                _lutTan[i] = wt;
            }

            _bakedLength = Mathf.Max(0.01f, Container.CalculateLength());
        }

        /// <summary>
        /// Bake centerline kiểu "cạnh thẳng, bo góc": nối các knot bằng đoạn thẳng,
        /// chèn cung tròn tại mỗi góc, rồi resample đều theo CHIỀU DÀI CUNG vào LUT.
        /// Trả về false nếu polyline suy biến (để Bake() fallback về spline gốc).
        /// </summary>
        bool BakeStraight()
        {
            if (!BuildRoundedCenterline(_pathPts)) return false;

            int m = _pathPts.Count;
            if (m < 2) return false;

            // Cộng dồn chiều dài tới từng điểm polyline.
            float total = 0f;
            for (int i = 1; i < m; i++)
                total += Vector3.Distance(_pathPts[i - 1], _pathPts[i]);
            if (total < 1e-4f) return false;

            int res = Mathf.Max(2, bakeResolution);
            EnsureLutArrays(res);

            int seg = 0;                 // chỉ số đoạn polyline hiện tại
            float segStart = 0f;         // chiều dài cộng dồn ở đầu đoạn seg
            float segLen = Mathf.Max(1e-6f, Vector3.Distance(_pathPts[0], _pathPts[1]));

            for (int i = 0; i <= res; i++)
            {
                float target = (i / (float)res) * total;

                // Tiến tới đoạn chứa 'target'.
                while (seg < m - 2 && target > segStart + segLen)
                {
                    segStart += segLen;
                    seg++;
                    segLen = Mathf.Max(1e-6f, Vector3.Distance(_pathPts[seg], _pathPts[seg + 1]));
                }

                float f = Mathf.Clamp01((target - segStart) / segLen);
                Vector3 a = _pathPts[seg];
                Vector3 b = _pathPts[seg + 1];

                Vector3 tan = b - a; tan.z = 0f;
                if (tan.sqrMagnitude < 1e-6f) tan = Vector3.right; else tan.Normalize();

                _lutPos[i] = Vector3.LerpUnclamped(a, b, f);
                _lutTan[i] = tan;
            }

            _bakedLength = Mathf.Max(0.01f, total);
            return true;
        }

        /// <summary>
        /// Dựng polyline (world space) gồm các đoạn thẳng nối knot + cung fillet ở các góc.
        /// </summary>
        bool BuildRoundedCenterline(List<Vector3> path)
        {
            path.Clear();

            var spline = Container.Spline;
            int kc = spline.Count;
            if (kc < 2) return false;

            bool closed = spline.Closed;

            // Knot world-space.
            var knots = new Vector3[kc];
            for (int i = 0; i < kc; i++)
                knots[i] = transform.TransformPoint((Vector3)spline[i].Position);

            int cseg = Mathf.Max(1, cornerSegments);

            for (int i = 0; i < kc; i++)
            {
                // Đầu/cuối của spline HỞ là điểm mút, không bo.
                bool isEndpoint = !closed && (i == 0 || i == kc - 1);
                if (isEndpoint) { AddPoint(path, knots[i]); continue; }

                int ip = (i - 1 + kc) % kc;
                int inx = (i + 1) % kc;
                Vector3 prev = knots[ip], cur = knots[i], next = knots[inx];

                Vector3 dirIn = cur - prev; float lenIn = dirIn.magnitude;
                Vector3 dirOut = next - cur; float lenOut = dirOut.magnitude;
                if (lenIn < 1e-5f || lenOut < 1e-5f) { AddPoint(path, cur); continue; }
                dirIn /= lenIn; dirOut /= lenOut;

                Vector3 a = -dirIn;   // tay hướng về knot trước
                Vector3 b = dirOut;   // tay hướng về knot sau
                float cosA = Mathf.Clamp(Vector3.Dot(a, b), -1f, 1f);
                float ang = Mathf.Acos(cosA);     // góc giữa 2 tay
                float half = ang * 0.5f;

                // Gần thẳng -> không cần bo.
                if (cornerRadius <= 1e-5f || half < 1e-3f || (Mathf.PI - ang) < 1e-3f)
                {
                    AddPoint(path, cur);
                    continue;
                }

                float tanHalf = Mathf.Tan(half);
                float r = cornerRadius;
                float d = r / tanHalf;                          // khoảng cách từ góc tới điểm tiếp xúc
                float maxD = Mathf.Min(lenIn, lenOut) * 0.5f;   // không cho cung lấn quá nửa đoạn
                if (d > maxD) { d = maxD; r = d * tanHalf; }

                Vector3 bis = a + b;
                if (bis.sqrMagnitude < 1e-6f) { AddPoint(path, cur); continue; }
                bis.Normalize();

                Vector3 tIn = cur - dirIn * d;        // điểm tiếp xúc trên đoạn vào
                Vector3 tOut = cur + dirOut * d;      // điểm tiếp xúc trên đoạn ra
                Vector3 center = cur + bis * (r / Mathf.Sin(half));

                Vector3 v0 = tIn - center;
                Vector3 v1 = tOut - center;
                for (int s = 0; s <= cseg; s++)
                {
                    float f = s / (float)cseg;
                    AddPoint(path, center + Vector3.Slerp(v0, v1, f));
                }
            }

            // Spline kín: nối điểm cuối về điểm đầu để resample khép vòng.
            if (closed && path.Count > 1) AddPoint(path, path[0]);

            return path.Count >= 2;
        }

        static void AddPoint(List<Vector3> path, Vector3 p)
        {
            // Bỏ điểm trùng liên tiếp để tránh đoạn dài 0.
            if (path.Count == 0 || (p - path[path.Count - 1]).sqrMagnitude > 1e-10f)
                path.Add(p);
        }

        void EnsureBaked() { if (!_baked) Bake(); }

        /// <summary>Tra LUT: trả vị trí TÂM + tiếp tuyến (đã chuẩn hoá) tại progress t (0..1).</summary>
        public bool TrySampleCenterline(float t, out Vector3 pos, out Vector3 tangent)
        {
            EnsureBaked();
            if (!_baked) { pos = transform.position; tangent = Vector3.right; return false; }

            float f = Mathf.Clamp01(t) * _bakedRes;
            int i0 = (int)f;
            if (i0 >= _bakedRes)
            {
                pos = _lutPos[_bakedRes];
                tangent = _lutTan[_bakedRes];
                return true;
            }

            float frac = f - i0;
            int i1 = i0 + 1;
            pos = Vector3.LerpUnclamped(_lutPos[i0], _lutPos[i1], frac);

            Vector3 tn = Vector3.Lerp(_lutTan[i0], _lutTan[i1], frac);
            if (tn.sqrMagnitude < 1e-6f) tn = Vector3.right; else tn.Normalize();
            tangent = tn;
            return true;
        }

        /// <summary>
        /// Vị trí world trên spline tại progress t (0..1), lệch sang ngang lateralOffset
        /// theo pháp tuyến (vuông góc hướng đi) trong mặt phẳng XY.
        /// </summary>
        public Vector3 GetPositionOnSpline(float t, float lateralOffset)
        {
            if (!TrySampleCenterline(t, out Vector3 pos, out Vector3 tan)) return transform.position;
            Vector3 normal = new Vector3(-tan.y, tan.x, 0f);
            return pos + normal * lateralOffset;
        }

        /// <summary>Hướng đi (tangent) đã chuẩn hoá tại t, trong mặt phẳng XY.</summary>
        public Vector3 GetTangent(float t)
        {
            if (!TrySampleCenterline(t, out _, out Vector3 tan)) return Vector3.right;
            return tan;
        }

        /// <summary>Chiều dài world của spline (cache; chỉ tính lại khi bake).</summary>
        public float GetSplineLength()
        {
            EnsureBaked();
            return _bakedLength;
        }

        // Vẽ 2 mép băng chuyền (cyan) khi chọn object trong Editor.
        // Dùng LUT đã bake để gizmo phản ánh đúng hình "cạnh thẳng, bo góc".
        void OnDrawGizmosSelected()
        {
            if (!HasSpline) return;
            EnsureBaked();
            if (!_baked) return;

            Gizmos.color = Color.cyan;
            const int seg = 96;
            Vector3 prevL = Vector3.zero, prevR = Vector3.zero;
            for (int i = 0; i <= seg; i++)
            {
                float t = i / (float)seg;
                Vector3 l = GetPositionOnSpline(t, +HalfWidth);
                Vector3 r = GetPositionOnSpline(t, -HalfWidth);
                if (i > 0)
                {
                    Gizmos.DrawLine(prevL, l);
                    Gizmos.DrawLine(prevR, r);
                }
                prevL = l; prevR = r;
            }
        }
    }
}
