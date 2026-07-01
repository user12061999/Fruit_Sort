using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

namespace FruitSort
{
    /// <summary>
    /// Cage/Bucket dùng SpriteGridFill: mỗi dot nhảy vào một ô rồi tăng fill của shader.
    /// Dot CÙNG MÀU đi vào VÙNG VA CHẠM (Collider2D chỉnh trong editor) sẽ bị hút vào và
    /// Không scale sprite và không tạo một SpriteRenderer cho từng ô.
    /// Đầy 100% -> punch scale (DOTween) -> Destroy. Cho phép nhiều bucket cùng màu.
    /// </summary>
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

        [Header("Fruit Visual Layers")]
        [Tooltip("Database dùng để lấy sprite và màu quả theo colorId.")]
        public FruitDatabase fruitDatabase;
        [Tooltip("Lớp nền luôn hiện đầy hình quả, nằm phía sau lớp fill.")]
        public SpriteRenderer background;
        public Color backgroundColor = new Color(1f, 1f, 1f, 0.2f);
        [Tooltip("Lớp phía trước dùng shader grid fill.")]
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

        [Header("Wrong color & return")]
        [Min(0f)] public float wrongColorLerpDuration = 0.25f;
        [Min(0f)] public float returnJumpPower = 0.8f;
        [Min(0.01f)] public float returnDuration = 0.35f;

        [Header("Nhả dot (release) — spawn ngay trong zone")]
        [Tooltip("Độ 'pop' nhẹ ngẫu nhiên khi dot xuất hiện trong zone (world unit/giây). 0 = đứng yên rồi rơi.")]
        [Min(0f)] public float releasePopSpeed = 2f;
        [Tooltip("Hệ số trọng lực để dot rơi nhẹ xuống băng chuyền trong zone.")]
        [Min(0f)] public float releaseGravityScale = 1f;
        [Tooltip("Giãn cách thời gian giữa từng dot khi nhả (0 = spawn cùng lúc).")]
        [Min(0f)] public float releaseInterval = 0.04f;

        // ---- runtime ----
        readonly List<Dot> _contained = new List<Dot>();
        readonly HashSet<Dot> _reserved = new HashSet<Dot>();
        int _containedColorId = -1;
        int _visibleFill;
        bool _full;
        bool _releasing;

        public bool IsActive => isActiveAndEnabled && !_full && !_releasing && currentFill < maxFill;
        public float FillRatio => maxFill > 0 ? Mathf.Clamp01(currentFill / (float)maxFill) : 1f;
        public Vector3 MouthPosition => mouth != null ? mouth.position : transform.position;
        public int ContainedColorId => _containedColorId;
        public bool IsReadyForPickup { get; private set; }
        public static event System.Action<Bucket> OnBucketFull;

        void OnEnable()
        {
            _containedColorId = currentFill > 0 ? colorId : -1;
            _visibleFill = Mathf.Clamp(currentFill, 0, Mathf.Max(1, maxFill));
            ApplyVisual();
            if (FallingPixelManager.Instance != null) FallingPixelManager.Instance.RegisterBucket(this);
        }

        void Update()
        {
            if (_full || currentFill <= 0 || Mouse.current == null ||
                !Mouse.current.leftButton.wasPressedThisFrame)
                return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 screen = Mouse.current.position.ReadValue();
            screen.z = Mathf.Abs(cam.transform.position.z - transform.position.z);
            if (Contains(cam.ScreenToWorldPoint(screen)))
                ReleaseContents();
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

        public bool CanAcceptColor(int incomingColorId)
        {
            if (!isActiveAndEnabled || _full || _releasing || currentFill + _reserved.Count >= maxFill)
                return false;
            return _containedColorId < 0 || _containedColorId == incomingColorId;
        }

        public bool TryReserve(Dot dot)
        {
            if (dot == null) return false;
            if (_reserved.Contains(dot)) return true;
            if (!CanAcceptColor(dot.colorId)) return false;

            if (_containedColorId < 0)
            {
                _containedColorId = dot.colorId;
                ApplyContainedColor(dot);
            }

            _reserved.Add(dot);
            return true;
        }

        public bool CanReceiveReserved(Dot dot)
        {
            return dot != null && _reserved.Contains(dot) && isActiveAndEnabled && !_full &&
                   currentFill < maxFill && dot.colorId == _containedColorId;
        }

        public void CancelReservation(Dot dot)
        {
            if (dot != null) _reserved.Remove(dot);
            ResetColorLockIfEmpty();
        }

        /// <summary>
        /// Bucket "nhận nuôi" 1 dot: parent vào giỏ, ném dot vào ô của nó (hiệu ứng xếp hoa quả),
        /// rồi tăng fill. Dot sẽ đi theo giỏ khi worker mang đi.
        /// </summary>
        public bool ReceiveDot(Dot d)
        {
            if (d == null) return false;
            if (!_reserved.Contains(d) && !TryReserve(d)) return false;
            if (!CanReceiveReserved(d))
            {
                CancelReservation(d);
                return false;
            }

            _reserved.Remove(d);
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
            return true;
        }

        void CompleteDotVisual(Dot dot)
        {
            _visibleFill = Mathf.Min(currentFill, _visibleFill + 1);
            UpdateFillVisual();
            if (dot != null)
            {
                if (dot.Sr != null) dot.Sr.enabled = false;
            }
        }

        /// <summary>Tăng fill. Đầy -> punch scale rồi Destroy.</summary>
        public void AddFill(int n)
        {
            if (_full || currentFill >= maxFill) return;
            currentFill = Mathf.Min(maxFill, currentFill + Mathf.Max(1, n));

            if (currentFill >= maxFill &&
                (_containedColorId < 0 || _containedColorId == colorId))
                DoFull();
        }

        public bool ReleaseContents()
        {
            if (_full || _releasing ||
                (currentFill <= 0 && _contained.Count == 0 && _reserved.Count == 0))
                return false;

            // Hủy đặt chỗ các dot đang bay tới (chưa vào giỏ) -> trả về belt.
            foreach (Dot dot in new List<Dot>(_reserved))
            {
                if (dot == null) continue;
                dot.targetBucket = null;
                dot.ignoredBucket = this;
                dot.state = DotState.OnBelt;
            }
            _reserved.Clear();

            List<Dot> returning = new List<Dot>(_contained);
            _contained.Clear();

            FallingPixelManager manager = FallingPixelManager.Instance;

            // Không có manager hoặc bucket đã tắt -> nhả tức thì, reset ngay.
            if (manager == null || !isActiveAndEnabled)
            {
                for (int i = 0; i < returning.Count; i++)
                {
                    if (returning[i] == null) continue;
                    if (manager != null) LaunchReleasedDot(returning[i], manager);
                    else Destroy(returning[i].gameObject);
                }
                ResetAfterRelease();
                return true;
            }

            // Nhả DẦN: mỗi dot ra zone thì fill vơi đúng 1/n (như ModelDotSpawner).
            StartCoroutine(ReleaseDotsRoutine(returning, manager));
            return true;
        }

        IEnumerator ReleaseDotsRoutine(List<Dot> returning, FallingPixelManager manager)
        {
            _releasing = true;
            WaitForSeconds wait = releaseInterval > 0f ? new WaitForSeconds(releaseInterval) : null;

            for (int i = 0; i < returning.Count; i++)
            {
                // Giảm dần fill theo từng dot.
                currentFill = Mathf.Max(0, currentFill - 1);
                _visibleFill = Mathf.Max(0, _visibleFill - 1);
                UpdateFillVisual();

                LaunchReleasedDot(returning[i], manager);

                if (wait != null && i + 1 < returning.Count) yield return wait;
            }

            ResetAfterRelease();
            _releasing = false;
        }

        void ResetAfterRelease()
        {
            currentFill = 0;
            _visibleFill = 0;
            _containedColorId = -1;
            UpdateFillVisual();
            SetBodyColor(Color.white, wrongColorLerpDuration);
        }

        void LaunchReleasedDot(Dot dot, FallingPixelManager manager)
        {
            if (dot == null) return;
            dot.ignoredBucket = this;

            // Spawn NGAY trong zone (điểm ngẫu nhiên), pop nhẹ ngẫu nhiên rồi rơi xuống belt trong zone.
            Vector3 origin = RandomReleasePosition();
            Vector2 velocity = Random.insideUnitCircle * releasePopSpeed;

            manager.ReleaseDotLaunched(dot, origin, velocity, this, releaseGravityScale);
        }

        /// <summary>Một điểm ngẫu nhiên BÊN TRONG zone (fallback: trong bán kính hút quanh miệng giỏ).</summary>
        Vector3 RandomReleasePosition()
        {
            if (zone != null)
            {
                Bounds b = zone.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 p = new Vector3(
                        Random.Range(b.min.x, b.max.x),
                        Random.Range(b.min.y, b.max.y),
                        MouthPosition.z);
                    if (!precisePointTest || zone.OverlapPoint(p)) return p;
                }
                return new Vector3(b.center.x, b.center.y, MouthPosition.z);
            }

            Vector2 r = Random.insideUnitCircle * attractRadius;
            return MouthPosition + new Vector3(r.x, r.y, 0f);
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

        public void RefreshVisuals()
        {
            ApplyVisual();
        }

        void ApplyVisual()
        {
            if (body == null) body = GetComponent<SpriteRenderer>();
            if (body != null)
            {
                FruitData fruit = fruitDatabase != null ? fruitDatabase.GetById(colorId) : null;
                if (fruit != null)
                {
                    color = fruit.color;
                    if (fruit.sprite != null) body.sprite = fruit.sprite;
                }

                body.enabled = true;
                body.color = Color.white;
                if (gridFill == null) gridFill = body.GetComponent<SpriteGridFill>();

                if (background != null)
                {
                    background.enabled = true;
                    background.sprite = body.sprite;
                    background.sortingLayerID = body.sortingLayerID;
                    background.sortingOrder = body.sortingOrder - 1;
                    background.color = backgroundColor;
                }
            }
            UpdateFillVisual();
        }

        void ApplyContainedColor(Dot dot)
        {
            if (dot.colorId == colorId)
            {
                SetBodyColor(Color.white, 0f);
                return;
            }

            SetBodyColor(dot.color, wrongColorLerpDuration);
        }

        void ResetColorLockIfEmpty()
        {
            if (currentFill > 0 || _contained.Count > 0 || _reserved.Count > 0) return;
            _containedColorId = -1;
            SetBodyColor(Color.white, wrongColorLerpDuration);
        }

        void SetBodyColor(Color target, float duration)
        {
            if (body == null) body = GetComponent<SpriteRenderer>();
            if (body == null) return;
            body.DOKill();
            if (duration <= 0f) body.color = target;
            else body.DOColor(target, duration).SetEase(Ease.Linear);
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
            if (background == null)
            {
                Transform backgroundTransform = transform.Find("Fruit Background");
                if (backgroundTransform != null)
                    background = backgroundTransform.GetComponent<SpriteRenderer>();
            }
            if (zone == null) zone = GetComponent<Collider2D>();
            maxFill = Mathf.Max(1, maxFill);
            gridColumns = Mathf.Max(1, gridColumns);
            cellGap = Mathf.Clamp(cellGap, 0f, 0.45f);
            wrongColorLerpDuration = Mathf.Max(0f, wrongColorLerpDuration);
            returnJumpPower = Mathf.Max(0f, returnJumpPower);
            returnDuration = Mathf.Max(0.01f, returnDuration);
            if (body != null && gridFill == null) gridFill = body.GetComponent<SpriteGridFill>();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(color.r, color.g, color.b, 0.4f);
            if (zone == null) Gizmos.DrawWireSphere(MouthPosition, attractRadius);
        }
    }
}
