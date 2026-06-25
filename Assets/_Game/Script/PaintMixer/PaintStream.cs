using UnityEngine;
using DG.Tweening;

namespace FruitSort
{
    /// <summary>
    /// Dòng sơn lỏng chạy trên băng chuyền. Có thể hợp nhất với PaintStream khác khi va chạm.
    /// Di chuyển bằng cách theo dõi progress trên ConveyorSpline (tương tự Dot).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class PaintStream : MonoBehaviour
    {
        [Header("Thuộc tính sơn")]
        [Tooltip("Thể tích sơn (0 = tự hủy).")]
        public float volume = 1f;

        [Tooltip("Vector CMY (x=Cyan, y=Magenta, z=Yellow) từ 0-1.")]
        public Vector3 cmy = new Vector3(1f, 0f, 0f);

        [Header("Di chuyển")]
        [Tooltip("Băng chuyền đang chạy trên đó.")]
        public ConveyorSpline conveyor;

        [Range(0f, 1f)]
        public float progress = 0f;

        [Header("Visual")]
        [Tooltip("Kích thước cơ sở của vòng tròn sơn.")]
        public float baseRadius = 0.25f;

        // Runtime state
        SpriteRenderer _sr;
        CircleCollider2D _col;
        bool _merging = false;
        bool _frozen = false;

        public bool IsFrozen { get => _frozen; set => _frozen = value; }
        public bool IsMerging => _merging;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _col = GetComponent<CircleCollider2D>();
            if (_col != null) _col.isTrigger = true;
        }

        void Start()
        {
            RefreshVisual();
        }

        void Update()
        {
            if (_frozen || _merging) return;
            if (conveyor == null) return;

            float speed = FallingPixelManager.Instance != null
                ? FallingPixelManager.Instance.beltSpeed
                : 1f;

            float length = conveyor.GetSplineLength();
            if (length > 0.001f)
                progress += (speed * Time.deltaTime) / length;

            // Băng chuyền là vòng kín (LoopSort): chạy hết thì lặp lại,
            // dòng sơn tiếp tục vòng quanh cho tới khi khớp màu 1 bucket.
            if (progress >= 1f)
                progress = Mathf.Repeat(progress, 1f);

            Vector3 pos = conveyor.GetPositionOnSpline(progress, 0f);
            transform.position = new Vector3(pos.x, pos.y, -0.1f);

            // Hướng theo tiếp tuyến băng chuyền
            Vector3 tan = conveyor.GetTangent(progress);
            if (tan.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(tan.y, tan.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        /// <summary>Màu RGB hiển thị tính từ CMY subtractive.</summary>
        public Color GetDisplayColor() => PaintColorUtility.CMYToRGB(cmy);

        /// <summary>
        /// Hợp nhất một PaintStream khác vào stream này (cộng volume, pha màu).
        /// Stream kia sẽ bị hủy.
        /// </summary>
        public void MergeWith(PaintStream other)
        {
            if (other == null || other == this || _merging) return;
            cmy = PaintColorUtility.MixCMY(cmy, volume, other.cmy, other.volume);
            volume += other.volume;
            other._merging = true;
            Destroy(other.gameObject);
            RefreshVisual();
            // Nhấp nháy khi hợp nhất
            _sr.DOColor(Color.white, 0.1f).OnComplete(() => _sr.color = GetDisplayColor());
        }

        /// <summary>Cập nhật màu SpriteRenderer và scale theo volume.</summary>
        public void RefreshVisual()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _sr.color = GetDisplayColor();

            // Scale theo thể tích (radius tăng theo căn bậc hai của volume)
            float r = Mathf.Clamp(baseRadius * Mathf.Sqrt(volume), 0.1f, 2f);
            transform.localScale = Vector3.one * (r / baseRadius);

            // Giữ collider = baseRadius (LOCAL). Bán kính world = baseRadius * scale = r.
            // KHÔNG đặt _col.radius = r vì transform đã scale -> sẽ nhân đôi (collider khổng lồ).
            if (_col != null) _col.radius = baseRadius;
        }

        /// <summary>Thêm một lượng CMY với volume vào stream này.</summary>
        public void AddPaint(Vector3 addCMY, float addVolume)
        {
            if (addVolume <= 0f) return;
            cmy = PaintColorUtility.MixCMY(cmy, volume, addCMY, addVolume);
            volume += addVolume;
            RefreshVisual();
        }

        /// <summary>Loại bỏ một kênh màu (0=C, 1=M, 2=Y).</summary>
        public void FilterChannel(int channel)
        {
            if (channel < 0 || channel > 2) return;
            cmy[channel] = 0f;
            RefreshVisual();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_merging || _frozen) return;
            var otherStream = other.GetComponent<PaintStream>();
            if (otherStream != null && !otherStream.IsMerging && !otherStream.IsFrozen)
                MergeWith(otherStream);
        }
    }
}
