using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace FruitSort
{
    /// <summary>
    /// Bọc một SplineContainer làm "băng chuyền".
    /// Cung cấp vị trí trên spline theo progress (0..1) + lệch ngang trong bề rộng băng chuyền.
    /// </summary>
    [RequireComponent(typeof(SplineContainer))]
    public class ConveyorSpline : MonoBehaviour
    {
        [Tooltip("Bề rộng băng chuyền (world unit). Dùng để random vị trí ban đầu và clamp lệch ngang.")]
        public float beltWidth = 3f;

        SplineContainer _container;
        public SplineContainer Container
        {
            get { if (_container == null) _container = GetComponent<SplineContainer>(); return _container; }
        }

        public float HalfWidth => beltWidth * 0.5f;

        bool HasSpline => Container != null && Container.Spline != null && Container.Spline.Count > 1;

        /// <summary>
        /// Vị trí world trên spline tại progress t (0..1), lệch sang ngang lateralOffset
        /// theo pháp tuyến (vuông góc hướng đi) trong mặt phẳng XY.
        /// </summary>
        public Vector3 GetPositionOnSpline(float t, float lateralOffset)
        {
            if (!HasSpline) return transform.position;

            t = Mathf.Clamp01(t);
            // SplineContainer.Evaluate trả về kết quả ở WORLD space (đã nhân transform).
            Container.Evaluate(t, out float3 pos, out float3 tan, out _);

            Vector3 worldPos = (Vector3)pos;
            Vector3 worldTan = (Vector3)tan;
            worldTan.z = 0f;
            if (worldTan.sqrMagnitude < 1e-6f) worldTan = Vector3.right;
            worldTan.Normalize();

            // Pháp tuyến 2D = xoay tangent 90°.
            Vector3 normal = new Vector3(-worldTan.y, worldTan.x, 0f);
            return worldPos + normal * lateralOffset;
        }

        /// <summary>Hướng đi (tangent) đã chuẩn hoá tại t, trong mặt phẳng XY.</summary>
        public Vector3 GetTangent(float t)
        {
            if (!HasSpline) return Vector3.right;
            Container.Evaluate(Mathf.Clamp01(t), out _, out float3 tan, out _);
            Vector3 worldTan = (Vector3)tan;
            worldTan.z = 0f;
            if (worldTan.sqrMagnitude < 1e-6f) return Vector3.right;
            return worldTan.normalized;
        }

        /// <summary>Chiều dài world của spline (đã tính transform).</summary>
        public float GetSplineLength()
        {
            if (!HasSpline) return 1f;
            return Mathf.Max(0.01f, Container.CalculateLength());
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
