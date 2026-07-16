using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

namespace FruitSort
{
    public enum BucketState
    {
        Empty,
        Receiving,
        Releasing,
        Completed,
        Disabled
    }

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
        [Tooltip("Kéo sprite quả của bucket vào đây. Nếu để trống, bucket sẽ lấy sprite từ Fruit Database theo Color Id.")]
        [InspectorName("Sprite Quả (Kéo Vào Đây)")]
        public SpriteRenderer fruitSprite;
        [Tooltip("Database dùng để lấy sprite và màu quả theo colorId.")]
        public FruitDatabase fruitDatabase;
        [Tooltip("Lớp nền luôn hiện đầy hình quả, nằm phía sau lớp fill.")]
        public SpriteRenderer background;
        public Color backgroundColor = new Color(1f, 1f, 1f, 0.2f);
        [Tooltip("Lớp phía trước dùng shader grid fill.")]
        public SpriteRenderer body;
        [Tooltip("Component điều khiển shader grid fill trên cùng object với Body.")]
        public SpriteGridFill gridFill;
        [Tooltip("Khoảng trong suốt giữa các ô.")]
        [Range(0f, 0.45f)] public float cellGap = 0.02f;

        [Header("Hiển thị số dot")]
        [Tooltip("Text hiển thị 'số dot hiện tại/max'. Để trống = tự tạo TextMeshPro con lúc play.")]
        public TMP_Text countText;
        [Tooltip("Vị trí local của text so với tâm bucket (chỉ dùng khi text tự tạo).")]
        public Vector2 countTextOffset = Vector2.zero;
        [Tooltip("Cỡ chữ world-space của text tự tạo.")]
        [Min(0.5f)] public float countTextSize = 3f;
        [Tooltip("Cường độ punch scale của text khi số thay đổi (0 = tắt).")]
        [Range(0f, 1f)] public float countTextPop = 0.35f;
        [Tooltip("Thời lượng punch của text.")]
        [Min(0.05f)] public float countTextPopDuration = 0.2f;

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
        [Tooltip("Cường độ punch scale của giỏ mỗi khi 1 dot vào ô (0 = tắt).")]
        [Range(0f, 0.3f)] public float receivePunch = 0.06f;

        [Header("Wrong color")]
        [Tooltip("BẬT (mặc định): chỉ cho click-nhả khi giỏ đang chứa SAI màu. " +
                 "Chặn misclick đổ nhầm giỏ đúng màu (vừa tốn move vừa mất tiến độ).")]
        public bool releaseOnlyWrongColor = true;
        [Min(0f)] public float wrongColorLerpDuration = 0.25f;
        [Tooltip("Icon gợi ý click-để-nhả (tuỳ chọn, kéo SpriteRenderer con vào). " +
                 "Tự hiện + nhấp nháy khi bucket chứa dot sai màu.")]
        public SpriteRenderer releaseHintIcon;
        [Tooltip("Biên độ nhấp nháy alpha của background khi chứa màu sai.")]
        [Range(0f, 0.8f)] public float wrongColorBlink = 0.35f;

        [Header("Phóng dot vào băng chuyền")]
        [Tooltip("Hướng phóng từ miệng bucket (sẽ normalize).")]
        public Vector2 launchDirection = Vector2.down;
        [Tooltip("Tốc độ phóng (world unit/giây).")]
        [Min(0.1f)] public float launchSpeed = 10f;
        [Tooltip("Độ tản hướng ngẫu nhiên (±độ).")]
        [Range(0f, 45f)] public float launchSpread = 3f;
        [Tooltip("Giãn cách thời gian giữa từng dot khi nhả (0 = spawn cùng lúc).")]
        [Min(0f)] public float releaseInterval = 0.04f;

        // ---- runtime ----
        readonly List<Dot> _contained = new List<Dot>();
        readonly HashSet<Dot> _reserved = new HashSet<Dot>();
        readonly Dictionary<int, int> _pendingReleaseByColor = new Dictionary<int, int>();
        static readonly List<Bucket> s_all = new List<Bucket>();
        int _containedColorId = -1;
        int _visibleFill;
        bool _full;
        bool _releasing;

        public int TargetColorId => colorId;
        public int? LockedColorId => _containedColorId >= 0 ? _containedColorId : null;
        public int CurrentFill => currentFill;
        public int VisibleFill => _visibleFill;
        public int ReservedCount => _reserved.Count;
        public BucketState State
        {
            get
            {
                if (!isActiveAndEnabled) return BucketState.Disabled;
                if (_full) return BucketState.Completed;
                if (_releasing) return BucketState.Releasing;
                return currentFill > 0 || _reserved.Count > 0 ? BucketState.Receiving : BucketState.Empty;
            }
        }
        public bool IsActive => isActiveAndEnabled && !_full && !_releasing && currentFill < maxFill;
        public float FillRatio => maxFill > 0 ? Mathf.Clamp01(currentFill / (float)maxFill) : 1f;
        public Vector3 MouthPosition => mouth != null ? mouth.position : transform.position;
        public int ContainedColorId => _containedColorId;
        public bool IsFull => _full;
        public int RemainingFillForWin => Mathf.Max(0, maxFill - currentFill);
        public bool CanStillFillForWin =>
            isActiveAndEnabled && !_full && !_releasing && currentFill < maxFill &&
            (_containedColorId < 0 || _containedColorId == colorId);
        public bool IsReadyForPickup { get; private set; }
        public static event System.Action<Bucket> OnBucketFull;

        /// <summary>Mọi bucket đang bật trong scene (registry — thay cho FindObjectsByType mỗi frame).</summary>
        public static IReadOnlyList<Bucket> All => s_all;

        void OnEnable()
        {
            if (!s_all.Contains(this)) s_all.Add(this);
            _containedColorId = currentFill > 0 ? colorId : -1;
            _visibleFill = Mathf.Clamp(currentFill, 0, Mathf.Max(1, maxFill));
            ApplyVisual();
            if (FallingPixelManager.Instance != null) FallingPixelManager.Instance.RegisterBucket(this);
        }

        void Update()
        {
            // Popup đang đè -> không nhận click gameplay (input là polling, UI không chặn được).
            if (GameplayPause.IsPaused) return;

            // Cho phép click cả khi mới chỉ có dot ĐANG BAY TỚI (_reserved) để hủy kịp.
            if (_full || (currentFill <= 0 && _reserved.Count == 0) ||
                !PointerInput.PressedThisFrame())
                return;

            // Giỏ đang giữ ĐÚNG màu -> nhả ra chỉ có hại (tốn move + mất tiến độ).
            if (releaseOnlyWrongColor && _containedColorId == colorId) return;

            // UI đang đè lên -> không cho click xuyên qua xuống bucket.
            if (UIPointerGuard.IsPointerOverUI()) return;

            Camera cam = Camera.main;
            if (cam == null) return;
            if (!PointerInput.TryGetPosition(out Vector2 pointerPos)) return;

            Vector3 screen = pointerPos;
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
            CancelAllReservations();
            s_all.Remove(this);
            _pendingReleaseByColor.Clear();
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
            if (dot.targetBucket != null && dot.targetBucket != this) return false;
            if (!CanAcceptColor(dot.colorId)) return false;

            if (_containedColorId < 0)
            {
                _containedColorId = dot.colorId;
                ApplyContainedColor(dot);
            }

            _reserved.Add(dot);
            dot.targetBucket = this;
            return true;
        }

        public bool CanReceiveReserved(Dot dot)
        {
            return dot != null && _reserved.Contains(dot) && isActiveAndEnabled && !_full &&
                   currentFill < maxFill && dot.colorId == _containedColorId;
        }

        public void CancelReservation(Dot dot)
        {
            if (dot != null)
            {
                _reserved.Remove(dot);
                if (dot.targetBucket == this) dot.targetBucket = null;
            }
            ResetColorLockIfEmpty();
        }

        void CancelAllReservations()
        {
            foreach (Dot dot in _reserved)
            {
                if (dot == null) continue;
                if (dot.targetBucket == this) dot.targetBucket = null;
                dot.ignoredBucket = this;
                dot.state = DotState.OnBelt;
            }
            _reserved.Clear();
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
            if (d.targetBucket == this) d.targetBucket = null;
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

            Vector3 targetWorld = gridFill != null ? gridFill.GetCellWorldPosition(GetLandingCellIndex(slot)) : MouthPosition;
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

            // Punch nhẹ mỗi khi 1 dot vào ô. KHÔNG chạy khi đã full để không đè lên
            // cú punch lớn của DoFull.
            if (!_full && receivePunch > 0f)
            {
                transform.DOKill(true); // hoàn tất punch dở để không lệch scale gốc
                transform.DOPunchScale(Vector3.one * receivePunch, 0.15f, 6, 0.7f);
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
            GamePlayManager gamePlay = GamePlayManager.Instance;
            if (gamePlay != null && !gamePlay.CanUseMove)
                return false;

            if (_full || _releasing ||
                (currentFill <= 0 && _contained.Count == 0 && _reserved.Count == 0))
                return false;

            // Hủy đặt chỗ các dot đang bay tới (chưa vào giỏ) -> trả về belt.
            CancelAllReservations();

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
                if (gamePlay != null) gamePlay.RecordInteraction();
                return true;
            }

            // Nhả DẦN: mỗi dot ra zone thì fill vơi đúng 1/n (như ModelDotSpawner).
            TrackPendingRelease(returning);
            StartCoroutine(ReleaseDotsRoutine(returning, manager));
            if (gamePlay != null) gamePlay.RecordInteraction();
            return true;
        }

        IEnumerator ReleaseDotsRoutine(List<Dot> returning, FallingPixelManager manager)
        {
            _releasing = true;
            WaitForSeconds wait = releaseInterval > 0f ? new WaitForSeconds(releaseInterval) : null;

            for (int i = 0; i < returning.Count; i++)
            {
                // Popup đè lên giữa lúc nhả -> đứng chờ, không phóng dot trong lúc pause.
                while (GameplayPause.IsPaused) yield return null;

                // Giảm dần fill theo từng dot.
                currentFill = Mathf.Max(0, currentFill - 1);
                _visibleFill = Mathf.Max(0, _visibleFill - 1);
                UpdateFillVisual();

                UntrackPendingRelease(returning[i]);
                LaunchReleasedDot(returning[i], manager);

                if (wait != null && i + 1 < returning.Count) yield return wait;
            }

            _pendingReleaseByColor.Clear();
            ResetAfterRelease();
            _releasing = false;
        }

        void TrackPendingRelease(List<Dot> returning)
        {
            _pendingReleaseByColor.Clear();
            for (int i = 0; i < returning.Count; i++)
            {
                Dot dot = returning[i];
                if (dot == null) continue;
                if (_pendingReleaseByColor.TryGetValue(dot.colorId, out int count))
                    _pendingReleaseByColor[dot.colorId] = count + 1;
                else
                    _pendingReleaseByColor.Add(dot.colorId, 1);
            }
        }

        void UntrackPendingRelease(Dot dot)
        {
            if (dot == null) return;
            if (!_pendingReleaseByColor.TryGetValue(dot.colorId, out int count)) return;

            if (count <= 1) _pendingReleaseByColor.Remove(dot.colorId);
            else _pendingReleaseByColor[dot.colorId] = count - 1;
        }

        /// <summary>
        /// Đếm dot màu <paramref name="colorId"/> đang nằm NHẦM trong các giỏ khác màu
        /// (giỏ giữ màu sai — người chơi có thể click nhả để lấy lại). Dùng cho cảnh báo
        /// cung/cầu dot của GamePlayManager; KHÔNG dùng cho check thua vì nhả tốn move.
        /// </summary>
        public static int CountMisplacedDotsForColor(int colorId)
        {
            int total = 0;
            for (int i = 0; i < s_all.Count; i++)
            {
                Bucket bucket = s_all[i];
                if (bucket == null || !bucket.isActiveAndEnabled) continue;
                // Giỏ đang giữ đúng màu của nó -> số dot đó đã tính vào tiến độ fill.
                if (bucket._containedColorId < 0 || bucket._containedColorId == bucket.colorId) continue;
                if (bucket._containedColorId != colorId) continue;
                total += bucket._contained.Count;
            }
            return total;
        }

        public static int CountPendingReleaseDotsForColor(int colorId)
        {
            int total = 0;
            for (int i = 0; i < s_all.Count; i++)
            {
                Bucket bucket = s_all[i];
                if (bucket == null || !bucket.isActiveAndEnabled) continue;
                if (bucket._pendingReleaseByColor.TryGetValue(colorId, out int count))
                    total += count;
            }
            return total;
        }

        /// <summary>Ghi colorId các dot ĐÃ nằm trong giỏ vào buffer (phục vụ save tiến trình).</summary>
        public void GetContainedColorIds(List<int> buffer)
        {
            if (buffer == null) return;
            for (int i = 0; i < _contained.Count; i++)
                if (_contained[i] != null) buffer.Add(_contained[i].colorId);
        }

        /// <summary>
        /// Ghi colorId các dot đang chờ nhả dở (coroutine <see cref="ReleaseContents"/>) vào buffer.
        /// Các dot này KHÔNG còn trong _contained và CHƯA lên belt -> save như dot bay tại miệng giỏ.
        /// </summary>
        public void AppendPendingReleaseColors(List<int> buffer)
        {
            if (buffer == null) return;
            foreach (KeyValuePair<int, int> kv in _pendingReleaseByColor)
                for (int i = 0; i < kv.Value; i++) buffer.Add(kv.Key);
        }

        /// <summary>
        /// Khôi phục dot trong giỏ từ save: tạo dot ở trạng thái đã "nhận nuôi" xong
        /// (nằm đúng ô, sprite ẩn, shader fill hiện ô) — không chạy hiệu ứng bay vào.
        /// Gọi ngay sau khi level vừa dựng (giỏ đang rỗng).
        /// </summary>
        public void RestoreContents(IList<int> containedColorIds, Dot dotPrefab, float dotScale)
        {
            if (containedColorIds == null || containedColorIds.Count == 0 || dotPrefab == null) return;

            Transform root = contentRoot != null ? contentRoot : transform;
            int count = Mathf.Min(containedColorIds.Count, maxFill - currentFill);
            for (int i = 0; i < count; i++)
            {
                int cid = containedColorIds[i];
                if (_containedColorId < 0) _containedColorId = cid;

                FallingPixelManager manager = FallingPixelManager.Instance;
                Dot d = manager != null
                    ? manager.AcquireDot(dotPrefab, MouthPosition, Quaternion.identity)
                    : Instantiate(dotPrefab);
                if (d == null) continue;
                FruitData fruit = fruitDatabase != null ? fruitDatabase.GetById(cid) : null;
                d.Init(cid, fruit != null ? fruit.color : color, 1, new Vector2Int(-1, -1));
                d.transform.localScale = Vector3.one * Mathf.Max(0.01f, dotScale);
                d.capturedByBucket = true;
                d.sortScoreAwarded = true; // điểm của dot này đã nằm trong score lưu kèm save
                d.transform.SetParent(root, true);
                d.transform.position = gridFill != null ? gridFill.GetCellWorldPosition(GetLandingCellIndex(_contained.Count)) : MouthPosition;
                d.transform.rotation = Quaternion.identity;
                if (d.Sr != null) d.Sr.enabled = false; // như CompleteDotVisual khi dot đã vào ô

                _contained.Add(d);
                currentFill++;
                _visibleFill++;
            }

            UpdateFillVisual();

            // Giỏ đang giữ màu sai -> tint lại như lúc nhận dot sai màu (kèm blink gợi ý nhả).
            if (_containedColorId >= 0 && _containedColorId != colorId)
            {
                FruitData wrong = fruitDatabase != null ? fruitDatabase.GetById(_containedColorId) : null;
                SetBodyColor(wrong != null ? wrong.color : Color.gray, 0f);
            }
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
            manager.LaunchDot(dot, MouthPosition, launchDirection, launchSpeed, launchSpread, this);

            // Pop scale nhẹ để dot "bật" ra khỏi giỏ thay vì hiện đột ngột.
            Vector3 fullScale = dot.transform.localScale;
            dot.transform.localScale = fullScale * 0.6f;
            dot.transform.DOScale(fullScale, 0.18f)
               .SetEase(Ease.OutBack)
               .SetLink(dot.gameObject);
        }

        void DoFull()
        {
            _full = true;
            if (GamePlayManager.Instance != null) GamePlayManager.Instance.OnBucketFilled(this);
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
                    if (fruitSprite != null) fruitSprite.sprite = fruit.sprite;
                    // Lớp fill (body) cũng dùng art quả -> background đổi theo (copy body.sprite bên dưới).
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
            if (body != null)
            {
                body.DOKill();
                if (duration <= 0f) body.color = target;
                else body.DOColor(target, duration).SetEase(Ease.Linear);
            }

            bool wrong = target != Color.white;

            // Body chỉ hiện theo fill (0 lúc rỗng) -> tint thêm BACKGROUND (luôn hiện đầy)
            // để thấy màu sai ngay cả khi chưa có ô nào reveal. Sai màu -> NHẤP NHÁY alpha
            // để gợi ý người chơi click nhả giỏ.
            if (background != null)
            {
                background.DOKill();
                if (wrong)
                {
                    background.color = new Color(target.r, target.g, target.b, backgroundColor.a);
                    if (wrongColorBlink > 0f)
                        background.DOFade(Mathf.Min(1f, backgroundColor.a + wrongColorBlink), 0.45f)
                                  .SetLoops(-1, LoopType.Yoyo)
                                  .SetEase(Ease.InOutSine);
                }
                else
                {
                    if (duration <= 0f) background.color = backgroundColor;
                    else background.DOColor(backgroundColor, duration).SetEase(Ease.Linear);
                }
            }

            // Icon gợi ý (nếu designer gán): hiện + nhấp nháy khi sai màu, ẩn khi hết.
            if (releaseHintIcon != null)
            {
                releaseHintIcon.DOKill();
                releaseHintIcon.enabled = wrong;
                if (wrong)
                {
                    Color c = releaseHintIcon.color; c.a = 1f;
                    releaseHintIcon.color = c;
                    releaseHintIcon.DOFade(0.25f, 0.4f)
                                   .SetLoops(-1, LoopType.Yoyo)
                                   .SetEase(Ease.InOutSine);
                }
            }
        }

        /// <summary>
        /// Map index dot (0..maxFill-1) sang index ô trên lưới cân bằng: lưới có thể nhiều ô
        /// hơn số dot, nên dot phải đáp vào ô nằm tại mép fill tương ứng của shader.
        /// </summary>
        int GetLandingCellIndex(int slot)
        {
            if (gridFill == null) return slot;
            int totalCells = Mathf.Max(1, gridFill.Columns * gridFill.Rows);
            int max = Mathf.Max(1, maxFill);
            return Mathf.Clamp(Mathf.FloorToInt((slot + 0.5f) * totalCells / max), 0, totalCells - 1);
        }

        void UpdateFillVisual()
        {
            UpdateCountText();
            if (gridFill == null) return;
            // Lưới vuông cân bằng (rows = columns), không cần khớp đúng số dot.
            gridFill.SetBalancedGrid(maxFill);
            gridFill.CellGap = cellGap;
            gridFill.FillAmount = Mathf.Clamp01(_visibleFill / (float)Mathf.Max(1, maxFill));
        }

        int _lastShownCount = int.MinValue;

        void UpdateCountText()
        {
            EnsureCountText();
            if (countText == null) return;

            int shown = Mathf.Clamp(_visibleFill, 0, maxFill);
            countText.text = $"{shown}/{maxFill}";

            // Pop khi GIÁ TRỊ đổi (bỏ qua lần set đầu để không pop lúc vừa dựng level).
            if (Application.isPlaying && countTextPop > 0f &&
                _lastShownCount != int.MinValue && shown != _lastShownCount)
            {
                countText.transform.DOKill(true); // hoàn tất punch dở để không lệch scale gốc
                countText.transform.DOPunchScale(Vector3.one * countTextPop, countTextPopDuration, 6, 0.7f);
            }
            _lastShownCount = shown;
        }

        // Tự tạo TextMeshPro world-space khi chưa gán trong prefab (chỉ lúc play,
        // tránh đẻ object rác vào scene/prefab trong edit mode).
        void EnsureCountText()
        {
            if (countText != null || !Application.isPlaying) return;

            var go = new GameObject("CountText");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = (Vector3)countTextOffset;

            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.fontSize = countTextSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(2f, 1f);

            MeshRenderer textRenderer = go.GetComponent<MeshRenderer>();
            if (textRenderer != null && body != null)
            {
                textRenderer.sortingLayerID = body.sortingLayerID;
                textRenderer.sortingOrder = body.sortingOrder + 20;
            }

            countText = tmp;
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
            cellGap = Mathf.Clamp(cellGap, 0f, 0.45f);
            wrongColorLerpDuration = Mathf.Max(0f, wrongColorLerpDuration);
            launchSpeed = Mathf.Max(0.1f, launchSpeed);
            launchSpread = Mathf.Clamp(launchSpread, 0f, 45f);
            if (body != null && gridFill == null) gridFill = body.GetComponent<SpriteGridFill>();
            ApplyVisual();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(color.r, color.g, color.b, 0.4f);
            if (zone == null) Gizmos.DrawWireSphere(MouthPosition, attractRadius);
        }
    }
}
