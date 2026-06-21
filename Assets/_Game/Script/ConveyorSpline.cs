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
    /// </summary>
    [RequireComponent(typeof(SplineContainer))]
    public class ConveyorSpline : MonoBehaviour
    {
        [Tooltip("Bề rộng băng chuyền (world unit). Dùng để random vị trí ban đầu và clamp lệch ngang.")]
        public float beltWidth = 3f;

        [Tooltip("Số điểm sample khi bake LUT. Cao hơn = mượt hơn nhưng tốn RAM/tải bake. 64-128 là đủ cho hầu hết băng chuyền.")]
        [Min(2)] public int bakeResolution = 96;

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

        /// <summary>Bake (hoặc re-bake) LUT từ spline hiện tại. Có thể gọi tay nếu sửa spline lúc runtime.</summary>
        public void Bake()
        {
            if (!HasSpline) { _baked = false; return; }

            int res = Mathf.Max(2, bakeResolution);
            if (_lutPos == null || _bakedRes != res)
            {
                _lutPos = new Vector3[res + 1];
                _lutTan = new Vector3[res + 1];
                _bakedRes = res;
            }

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
            _baked = true;
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
        void OnDrawGizmosSelected()
        {
            if (!HasSpline) return;
            Gizmos.color = Color.cyan;
            const int seg = 48;
            Vector3 prevL = Vector3.zero, prevR = Vector3.zero;
            for (int i = 0; i <= seg; i++)
            {
                float t = i / (float)seg;
                // Gizmo dùng Evaluate trực tiếp để luôn đúng kể cả khi chưa bake trong editor.
                Container.Evaluate(t, out float3 p, out float3 tn, out _);
                Vector3 wt = (Vector3)tn; wt.z = 0f;
                if (wt.sqrMagnitude < 1e-6f) wt = Vector3.right; else wt.Normalize();
                Vector3 normal = new Vector3(-wt.y, wt.x, 0f);
                Vector3 l = (Vector3)p + normal * HalfWidth;
                Vector3 r = (Vector3)p - normal * HalfWidth;
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
