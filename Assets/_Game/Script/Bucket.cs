using UnityEngine;
using DG.Tweening;

namespace FruitSort
{
    /// <summary>
    /// Thùng phân loại: có ID màu và tỉ lệ Fill (cần maxFill dot để đầy).
    /// Dot CÙNG MÀU đi vào VÙNG VA CHẠM (Collider2D chỉnh trong editor) sẽ bị hút vào và
    /// fill dần SPRITE chính của thùng (qua shader FruitSort/SpriteFill, _FillAmount).
    /// Đầy 100% -> punch scale (DOTween) -> Destroy. Cho phép nhiều bucket cùng màu.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
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

        [Header("Vùng va chạm (chỉnh trong editor)")]
        [Tooltip("Collider2D làm vùng phát hiện dot. Để trống = tự lấy Collider2D trên object, " +
                 "không có thì fallback dùng attractRadius. Nên đặt 'Is Trigger' = true.")]
        public Collider2D zone;
        [Tooltip("Bán kính hút dự phòng khi KHÔNG gán zone.")]
        public float attractRadius = 1.2f;
        [Tooltip("Kiểm tra chính xác theo hình collider (OverlapPoint, có gọi Physics2D). " +
                 "TẮT (mặc định) = chỉ kiểm tra AABB bounds, rẻ hơn nhiều, hợp cho zone hình hộp.")]
        public bool precisePointTest = false;

        [Header("Hút dot")]
        [Tooltip("Điểm hút (miệng thùng). Để trống = dùng vị trí của bucket.")]
        public Transform mouth;
        [Tooltip("Tốc độ kéo dot vào (world unit/giây).")]
        public float attractSpeed = 6f;

        [Header("Hiển thị")]
        [Tooltip("Sprite chính của bucket (sẽ fill dần). Để trống sẽ tự lấy SpriteRenderer.")]
        public SpriteRenderer body;
        [Tooltip("Thanh fill phụ (tuỳ chọn): scale Y theo % đầy.")]
        public Transform fillBar;

        [Header("Hành động khi đầy")]
        [Tooltip("Cường độ punch scale khi đầy.")]
        public float punchScale = 0.35f;
        [Tooltip("Thời lượng punch trước khi destroy.")]
        public float punchDuration = 0.4f;

        [Header("Xếp dot vào giỏ (như xếp hoa quả)")]
        [Tooltip("Gốc để xếp dot (đáy giỏ). Để trống = dùng transform của bucket.")]
        public Transform contentRoot;
        [Tooltip("Offset của ô đầu tiên so với contentRoot (local).")]
        public Vector2 slotAreaOffset = Vector2.zero;
        [Tooltip("Số dot mỗi hàng trong giỏ.")]
        [Min(1)] public int slotColumns = 3;
        [Tooltip("Khoảng cách giữa các ô (local).")]
        public float slotSpacing = 0.3f;
        [Tooltip("Scale của dot khi đã nằm trong giỏ.")]
        public float dotScaleInBucket = 0.35f;
        [Tooltip("Độ cao cú nảy khi dot bay vào giỏ (hiệu ứng ném vào).")]
        public float jumpPower = 0.7f;
        [Tooltip("Thời lượng dot bay vào ô của nó.")]
        public float dropDuration = 0.35f;

        // ---- runtime ----
        [System.NonSerialized] public BucketSpawner spawner;     // ai sinh ra bucket này
        [System.NonSerialized] public int spawnerSlotIndex = -1; // ô tương ứng để tái sinh
        readonly System.Collections.Generic.List<Dot> _contained = new System.Collections.Generic.List<Dot>();
        MaterialPropertyBlock _mpb;
        static readonly int FillAmountID = Shader.PropertyToID("_FillAmount");
        bool _full;

        public bool IsActive => isActiveAndEnabled && !_full && currentFill < maxFill;
        public float FillRatio => maxFill > 0 ? Mathf.Clamp01(currentFill / (float)maxFill) : 1f;
        public Vector3 MouthPosition => mouth != null ? mouth.position : transform.position;
        public bool IsReadyForPickup { get; private set; }
        public static event System.Action<Bucket> OnBucketFull;

        void OnEnable()
        {
            ApplyVisual();
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

        /// <summary>Dot ở world position này có nằm trong vùng bắt của bucket không?</summary>
        public bool Contains(Vector3 worldPos)
        {
            if (zone != null)
            {
                // Đường nhanh: AABB bounds (chỉ là so sánh số, KHÔNG gọi Physics2D).
                if (!zone.bounds.Contains(new Vector3(worldPos.x, worldPos.y, zone.bounds.center.z)))
                    return false;
                // Chỉ khi đã trong AABB mới (tuỳ chọn) test chính xác theo hình.
                return !precisePointTest || zone.OverlapPoint(worldPos);
            }
            // Không có zone: dùng bình phương khoảng cách (tránh Sqrt).
            float dx = worldPos.x - MouthPosition.x;
            float dy = worldPos.y - MouthPosition.y;
            return dx * dx + dy * dy <= attractRadius * attractRadius;
        }

        /// <summary>
        /// Bucket "nhận nuôi" 1 dot: parent vào giỏ, ném dot vào ô của nó (hiệu ứng xếp hoa quả),
        /// rồi tăng fill. Dot sẽ đi theo giỏ khi worker mang đi.
        /// </summary>
        public void ReceiveDot(Dot d)
        {
            if (d == null) return;
            Transform root = contentRoot != null ? contentRoot : transform;
            int slot = _contained.Count;
            _contained.Add(d);

            // Gỡ mọi điều khiển chuyển động cũ, parent vào giỏ (giữ vị trí world để bay vào mượt).
            d.transform.DOKill();
            d.transform.SetParent(root, true);

            // Đảm bảo dot vẽ ĐÈ lên thân giỏ và xếp lớp theo thứ tự vào.
            if (d.Sr != null)
            {
                if (body != null)
                {
                    d.Sr.sortingLayerID = body.sortingLayerID;
                    d.Sr.sortingOrder = body.sortingOrder + 1 + slot;
                }
            }

            Vector3 targetLocal = SlotLocalPosition(slot);
            d.transform.DOLocalJump(targetLocal, jumpPower, 1, dropDuration).SetEase(Ease.OutQuad);
            d.transform.DOScale(Vector3.one * dotScaleInBucket, dropDuration);
            d.transform.DOLocalRotate(Vector3.zero, dropDuration);

            AddFill(1);
        }

        /// <summary>Vị trí (local so với contentRoot) của ô thứ index trong giỏ — xếp theo hàng từ dưới lên.</summary>
        Vector3 SlotLocalPosition(int index)
        {
            int cols = Mathf.Max(1, slotColumns);
            int col = index % cols;
            int row = index / cols;
            float x = (col - (cols - 1) * 0.5f) * slotSpacing;
            float y = row * slotSpacing;
            return new Vector3(slotAreaOffset.x + x, slotAreaOffset.y + y, 0f);
        }

        /// <summary>Tăng fill. Đầy -> punch scale rồi Destroy.</summary>
        public void AddFill(int n)
        {
            if (_full || currentFill >= maxFill) return;
            currentFill = Mathf.Min(maxFill, currentFill + Mathf.Max(1, n));
            UpdateFillVisual();

            if (currentFill >= maxFill) DoFull();
        }

        void DoFull()
        {
            _full = true;
            if (GameManager.Instance != null) GameManager.Instance.OnBucketFilled(this);
            if (FallingPixelManager.Instance != null) FallingPixelManager.Instance.UnregisterBucket(this);

            OnBucketFull?.Invoke(this);

            // Punch scale; worker sẽ nhặt và destroy sau.
            transform.DOKill();
            transform.DOPunchScale(Vector3.one * punchScale, punchDuration, 8, 0.8f)
                     .SetUpdate(false)
                     .OnComplete(() => IsReadyForPickup = true);
        }

        /// <summary>Gọi khi BucketWorker bắt đầu nhặt thùng.</summary>
        public void BePickedUp()
        {
            IsReadyForPickup = false;
            if (zone != null) zone.enabled = false;
            // Worker vừa nhấc bucket khỏi ô -> tái sinh ngay 1 bucket rỗng cùng màu để không gián đoạn.
            if (spawner != null) spawner.RespawnSlot(spawnerSlotIndex);
        }

        /// <summary>Đặt màu/ID cho bucket (gọi bởi BucketSpawner khi sinh) và cập nhật hiển thị.</summary>
        public void Configure(int newColorId, Color newColor)
        {
            colorId = newColorId;
            color = newColor;
            ApplyVisual();
        }

        void ApplyVisual()
        {
            if (body == null) body = GetComponent<SpriteRenderer>();
            if (body != null) body.color = color;
            UpdateFillVisual();
        }

        void UpdateFillVisual()
        {
            // Fill chính sprite qua MaterialPropertyBlock (không tạo material instance, không leak).
            if (body != null)
            {
                if (_mpb == null) _mpb = new MaterialPropertyBlock();
                body.GetPropertyBlock(_mpb);
                _mpb.SetFloat(FillAmountID, FillRatio);
                body.SetPropertyBlock(_mpb);
            }

            // Thanh fill phụ tuỳ chọn.
            if (fillBar != null)
            {
                Vector3 s = fillBar.localScale;
                s.y = FillRatio;
                fillBar.localScale = s;
            }
        }

        void OnValidate()
        {
            if (body == null) body = GetComponent<SpriteRenderer>();
            if (zone == null) zone = GetComponent<Collider2D>();
            if (body != null) body.color = color;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(color.r, color.g, color.b, 0.4f);
            if (zone == null) Gizmos.DrawWireSphere(MouthPosition, attractRadius);
        }
    }
}
