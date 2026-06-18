using UnityEngine;

namespace FruitSort
{
    /// <summary>
    /// Thùng phân loại: có ID màu và tỉ lệ Fill (cần maxFill dot để đầy).
    /// Chỉ hút dot CÙNG MÀU khi dot đi vào attractRadius. Đầy 100% -> biến mất.
    /// Cho phép nhiều bucket cùng màu.
    /// </summary>
    public class Bucket : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("ID màu, khớp với colorId của Dot / index trong Palette.")]
        public int colorId = 0;
        public Color color = Color.white;

        [Header("Fill")]
        [Tooltip("Số dot cần để đầy.")]
        public int maxFill = 5;
        public int currentFill = 0;

        [Header("Hút dot")]
        [Tooltip("Điểm hút (miệng thùng). Để trống = dùng vị trí của bucket.")]
        public Transform mouth;
        [Tooltip("Tầm gần để bắt đầu hút dot.")]
        public float attractRadius = 1.2f;
        [Tooltip("Tốc độ kéo dot vào (world unit/giây).")]
        public float attractSpeed = 6f;

        [Header("Hiển thị (tuỳ chọn)")]
        [Tooltip("Sprite chính của bucket (để tô màu). Để trống sẽ tự lấy SpriteRenderer.")]
        public SpriteRenderer body;
        [Tooltip("Thanh fill: scale Y theo % đầy.")]
        public Transform fillBar;
        [Tooltip("Đầy thì Destroy (true) hay chỉ tắt (false).")]
        public bool destroyOnFull = false;

        public bool IsActive => isActiveAndEnabled && currentFill < maxFill;
        public float FillRatio => maxFill > 0 ? Mathf.Clamp01(currentFill / (float)maxFill) : 1f;
        public Vector3 MouthPosition => mouth != null ? mouth.position : transform.position;

        void OnEnable()
        {
            ApplyVisual();
            // Tự đăng ký với manager (manager có thể chưa sẵn -> Start sẽ thử lại).
            if (FallingPixelManager.Instance != null) FallingPixelManager.Instance.RegisterBucket(this);
        }

        void Start()
        {
            if (FallingPixelManager.Instance != null) FallingPixelManager.Instance.RegisterBucket(this);
        }

        void OnDisable()
        {
            if (FallingPixelManager.Instance != null) FallingPixelManager.Instance.UnregisterBucket(this);
        }

        /// <summary>Tăng fill. Đầy -> thông báo GameManager rồi biến mất.</summary>
        public void AddFill(int n)
        {
            if (currentFill >= maxFill) return;
            currentFill = Mathf.Min(maxFill, currentFill + Mathf.Max(1, n));
            UpdateFillBar();

            if (currentFill >= maxFill)
            {
                if (GameManager.Instance != null) GameManager.Instance.OnBucketFilled(this);
                if (FallingPixelManager.Instance != null) FallingPixelManager.Instance.UnregisterBucket(this);

                if (destroyOnFull) Destroy(gameObject);
                else gameObject.SetActive(false);
            }
        }

        void ApplyVisual()
        {
            if (body == null) body = GetComponent<SpriteRenderer>();
            if (body != null) body.color = color;
            UpdateFillBar();
        }

        void UpdateFillBar()
        {
            if (fillBar == null) return;
            Vector3 s = fillBar.localScale;
            s.y = FillRatio;
            fillBar.localScale = s;
        }

        void OnValidate()
        {
            if (body == null) body = GetComponent<SpriteRenderer>();
            if (body != null) body.color = color;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(color.r, color.g, color.b, 0.4f);
            Gizmos.DrawWireSphere(MouthPosition, attractRadius);
        }
    }
}
