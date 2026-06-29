using UnityEngine;
using DG.Tweening;

namespace FruitSort
{
    /// <summary>
    /// Cage/Bucket dùng SpriteGridFill: mỗi dot nhảy vào một ô rồi tăng fill của shader.
    /// Dot CÙNG MÀU đi vào VÙNG VA CHẠM (Collider2D chỉnh trong editor) sẽ bị hút vào và
    /// Không scale sprite và không tạo một SpriteRenderer cho từng ô.
    /// Đầy 100% -> punch scale (DOTween) -> Destroy. Cho phép nhiều bucket cùng màu.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Bucket : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("ID màu, khớp với colorId của Dot / index trong Palette.")]
        public int colorId = 0;
        public Color color = Color.white;

        [Header("Fill theo từng dot")]
        [Tooltip("Số dot cần để fill kín toàn bộ sprite. Mỗi dot nhận vào tăng đúng 1/n.")]
        [InspectorName("Dots For Full Sprite")]
        [Min(1)] public int maxFill = 5;
        [Tooltip("Số dot đã fill vào sprite (chỉ đọc tham khảo lúc play).")]
        [InspectorName("Filled Dots Debug")]
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

        [Header("Sprite Grid")]
        [Tooltip("SpriteRenderer hiển thị grid fill.")]
        public SpriteRenderer body;
        [Tooltip("Component điều khiển shader grid fill trên cùng object với Body.")]
        public SpriteGridFill gridFill;
        [Tooltip("Số ô mỗi hàng; số hàng tự tính từ Dots For Full Sprite.")]
        [Min(1)] public int gridColumns = 5;
        [Tooltip("Khoảng trong suốt giữa các ô.")]
        [Range(0f, 0.45f)] public float cellGap = 0.02f;

        [Header("Hành động khi đầy")]
        [Tooltip("Cường độ punch scale khi đầy.")]
        public float punchScale = 0.35f;
        [Tooltip("Thời lượng punch trước khi destroy.")]
        public float punchDuration = 0.4f;

        [Header("Xếp dot vào giỏ (như xếp hoa quả)")]
        [Tooltip("Gốc để xếp dot (đáy giỏ). Để trống = dùng transform của bucket.")]
        public Transform contentRoot;
        [Tooltip("Độ cao cú nảy khi dot bay vào giỏ (hiệu ứng ném vào).")]
        public float jumpPower = 0.7f;
        [Tooltip("Thời lượng dot bay vào ô của nó.")]
        public float dropDuration = 0.35f;

        // ---- runtime ----
        readonly System.Collections.Generic.List<Dot> _contained = new System.Collections.Generic.List<Dot>();
        int _visibleFill;
        bool _full;

        public bool IsActive => isActiveAndEnabled && !_full && currentFill < maxFill;
        public float FillRatio => maxFill > 0 ? Mathf.Clamp01(currentFill / (float)maxFill) : 1f;
        public Vector3 MouthPosition => mouth != null ? mouth.position : transform.position;
        public bool IsReadyForPickup { get; private set; }
        public static event System.Action<Bucket> OnBucketFull;

        void OnEnable()
        {
            _visibleFill = Mathf.Clamp(currentFill, 0, Mathf.Max(1, maxFill));
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
            int slot = currentFill;
            _contained.Add(d);

            // Gỡ mọi điều khiển chuyển động cũ, parent vào giỏ (giữ vị trí world để bay vào mượt).
            d.transform.DOKill();
            d.transform.SetParent(root, true);

            // Dot bay tới đúng ô; khi chạm ô thì shader mới reveal ô đó và dot thật được thu hồi.
            if (d.Sr != null)
            {
                if (body != null)
                {
                    d.Sr.sortingLayerID = body.sortingLayerID;
                    d.Sr.sortingOrder = body.sortingOrder + 1 + slot;
                }
            }

            Vector3 targetWorld = gridFill != null ? gridFill.GetCellWorldPosition(slot) : MouthPosition;
            Vector3 targetLocal = root.InverseTransformPoint(targetWorld);
            d.transform.DOLocalJump(targetLocal, jumpPower, 1, dropDuration)
                       .SetEase(Ease.OutQuad)
                       .OnComplete(() => CompleteDotVisual(d));
            d.transform.DOLocalRotate(Vector3.zero, dropDuration);

            AddFill(1);
        }

        void CompleteDotVisual(Dot dot)
        {
            _visibleFill = Mathf.Min(currentFill, _visibleFill + 1);
            UpdateFillVisual();
            if (dot != null)
            {
                _contained.Remove(dot);
                Destroy(dot.gameObject);
            }
        }

        /// <summary>Tăng fill. Đầy -> punch scale rồi Destroy.</summary>
        public void AddFill(int n)
        {
            if (_full || currentFill >= maxFill) return;
            currentFill = Mathf.Min(maxFill, currentFill + Mathf.Max(1, n));

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
        }

        void ApplyVisual()
        {
            if (body == null) body = GetComponent<SpriteRenderer>();
            if (body != null)
            {
                body.color = color;
                if (gridFill == null) gridFill = body.GetComponent<SpriteGridFill>();
            }
            UpdateFillVisual();
        }

        void UpdateFillVisual()
        {
            if (gridFill == null) return;
            int columns = Mathf.Max(1, gridColumns);
            int rows = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1, maxFill) / (float)columns));
            gridFill.SetGrid(columns, rows);
            gridFill.CellGap = cellGap;
            gridFill.FillAmount = Mathf.Clamp01(_visibleFill / (float)Mathf.Max(1, maxFill));
        }

        void OnValidate()
        {
            if (body == null) body = GetComponent<SpriteRenderer>();
            if (zone == null) zone = GetComponent<Collider2D>();
            maxFill = Mathf.Max(1, maxFill);
            gridColumns = Mathf.Max(1, gridColumns);
            cellGap = Mathf.Clamp(cellGap, 0f, 0.45f);
            if (body != null && gridFill == null) gridFill = body.GetComponent<SpriteGridFill>();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(color.r, color.g, color.b, 0.4f);
            if (zone == null) Gizmos.DrawWireSphere(MouthPosition, attractRadius);
        }
    }
}
