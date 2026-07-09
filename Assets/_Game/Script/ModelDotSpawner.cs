using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using TMPro;

namespace FruitSort
{
    /// <summary>
    /// Sprite package dùng shader grid fill. Mỗi dot spawn làm giảm Fill Amount đúng một dot.
    /// </summary>
    public class ModelDotSpawner : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("Prefab dot (giống dotPrefab của PixelGridManager).")]
        public Dot dotPrefab;
        [Tooltip("Để trống = tự dùng FallingPixelManager.Instance.")]
        public FallingPixelManager fallingManager;
        [Tooltip("Dùng để tra colorId và màu. Sprite của dot/spawner luôn giữ hình vuông mặc định.")]
        public FruitDatabase fruitDatabase;
        [Tooltip("Camera dùng để quy đổi vị trí click. Để trống = Camera.main.")]
        public Camera cam;
        [Tooltip("Sprite dùng làm khuôn chia grid. Để trống = tự lấy SpriteRenderer trên object này.")]
        public SpriteRenderer packageSprite;

        [Header("Gói (package)")]
        [Tooltip("Tổng số dot tương ứng với một sprite đầy. Mỗi dot spawn làm sprite vơi đúng 1/n giá trị này.")]
        [InspectorName("Dots For Full Sprite")]
        [Min(1)] public int totalClicks = 50;
        [Tooltip("Số dot còn lại trong sprite (chỉ đọc tham khảo lúc play).")]
        [InspectorName("Dots Left Debug")]
        public int clicksLeftDebug;

        [Header("Sprite Grid")]
        [Tooltip("Component điều khiển shader grid fill trên Package Sprite.")]
        public SpriteGridFill gridFill;
        [Tooltip("Khoảng trong suốt giữa các ô.")]
        [Range(0f, 0.45f)] public float cellGap = 0.02f;

        [Header("Hiển thị số dot")]
        [Tooltip("Text hiển thị số dot còn lại trong gói. Để trống = tự tạo TextMeshPro con lúc play.")]
        public TMP_Text countText;
        [Tooltip("Vị trí local của text so với tâm gói (chỉ dùng khi text tự tạo).")]
        public Vector2 countTextOffset = Vector2.zero;
        [Tooltip("Cỡ chữ world-space của text tự tạo.")]
        [Min(0.5f)] public float countTextSize = 3f;
        [Tooltip("Cường độ punch scale của text khi số thay đổi (0 = tắt).")]
        [Range(0f, 1f)] public float countTextPop = 0.35f;
        [Tooltip("Thời lượng punch của text.")]
        [Min(0.05f)] public float countTextPopDuration = 0.2f;

        [Header("Cấu hình spawn (mỗi lần click)")]
        [Tooltip("SỐ LƯỢNG dot sinh ra mỗi lần click.")]
        [Min(1)] public int spawnCount = 10;
        [Tooltip("Điểm gốc spawn. Để trống = vị trí của object này.")]
        public Transform spawnOrigin;
        [Tooltip("Bán kính rải ngẫu nhiên quanh điểm spawn để dot không chồng khít nhau.")]
        public float spawnSpread = 0.5f;
        [Tooltip("Scale mỗi dot (nên khớp dotSize của FallingPixelManager).")]
        public float dotScale = 0.5f;
        [Tooltip("HP của dot khi sinh (các dot này không bị bắn nên giá trị ít quan trọng).")]
        public int dotHP = 1;
        [Tooltip("Giãn cách thời gian (giây) giữa từng dot khi spawn loạt. 0 = sinh cùng lúc.")]
        public float spawnInterval = 0.03f;

        [Header("Phóng dot vào băng chuyền")]
        [Tooltip("Hướng phóng (sẽ normalize). Ví dụ: (0,-1) = thẳng xuống, (1,-1) = xuống phải.")]
        public Vector2 launchDirection = Vector2.down;
        [Tooltip("Tốc độ phóng ban đầu (world unit/giây).")]
        [Min(0.1f)] public float launchSpeed = 10f;
        [Tooltip("Độ tản hướng ngẫu nhiên (±độ). 0 = tất cả dot đi thẳng một hướng.")]
        [Range(0f, 45f)] public float launchSpread = 3f;

        [Header("Feedback")]
        [Tooltip("Cường độ punch scale khi click gói (0 = tắt).")]
        [Range(0f, 0.5f)] public float clickPunch = 0.12f;
        [Tooltip("Thời lượng punch.")]
        [Min(0.05f)] public float clickPunchDuration = 0.18f;

        [Header("Khi gói rỗng")]
        [Tooltip("Hàm chạy khi click hết gói (kéo-thả trong Inspector).")]
        public UnityEvent onDepleted;
        [Tooltip("Thời gian fade out trước khi ẩn gói.")]
        public float fadeOutDuration = 0.35f;

        [Header("Màu")]
        [Tooltip("Bảng màu. Index = colorId (khớp với colorId của Bucket).")]
        public Color[] palette = new Color[]
        {
            new Color(0.93f, 0.26f, 0.21f), // 0 đỏ
            new Color(0.30f, 0.69f, 0.31f), // 1 xanh lá
            new Color(0.13f, 0.59f, 0.95f), // 2 xanh dương
            new Color(1.00f, 0.76f, 0.03f), // 3 vàng
        };
        [Tooltip("Cố định 1 colorId (>=0) cho mọi dot. -1 = random theo palette.")]
        public int fixedColorId = -1;

        // ---- runtime ----
        int _dotsLeft;
        int _reservedDots;
        bool _depleted;

        /// <summary>Spawner đã rỗng gói chưa.</summary>
        public bool IsDepleted => _depleted;

        /// <summary>Số dot còn lại trong gói (phục vụ save tiến trình).</summary>
        public int DotsLeft => _dotsLeft;

        /// <summary>Khôi phục số dot còn lại từ save. 0 -> ẩn gói ngay, không hiệu ứng.</summary>
        public void RestoreDotsLeft(int dotsLeft)
        {
            _dotsLeft = Mathf.Clamp(dotsLeft, 0, Mathf.Max(1, totalClicks));
            _reservedDots = 0;
            clicksLeftDebug = _dotsLeft;
            UpdateFillVisual();
            if (_dotsLeft <= 0 && !_depleted)
            {
                _depleted = true;
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Tính lại gói theo totalClicks hiện tại. LevelBuilder PHẢI gọi sau khi gán config
        /// lúc runtime: Object.Instantiate đã chạy OnEnable TRƯỚC khi builder gán totalClicks,
        /// nên _dotsLeft lúc đó bị chốt theo giá trị mặc định của prefab.
        /// </summary>
        public void ReinitializePackage()
        {
            _dotsLeft = Mathf.Max(1, totalClicks);
            _reservedDots = 0;
            _depleted = false;
            clicksLeftDebug = _dotsLeft;
            RefreshVisuals();
        }

        /// <summary>Bật khi spawner được 1 ModelDotSpawnerColumn quản lý: nó sẽ KHÔNG tự xử lý
        /// click nữa (cột sẽ gọi DoClick thay).</summary>
        [System.NonSerialized] public bool managedExternally;

        // Mọi spawner đang bật. Click được xử lý TẬP TRUNG (1 lần/frame) để khi nhiều
        // spawner chồng nhau, CHỈ cái trên cùng (theo sorting order của sprite) spawn.
        static readonly List<ModelDotSpawner> s_all = new List<ModelDotSpawner>();
        static int s_lastClickFrame = -1;

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            if (packageSprite == null) packageSprite = GetComponent<SpriteRenderer>();
            if (fallingManager == null) fallingManager = FallingPixelManager.Instance;
            if (packageSprite != null && gridFill == null) gridFill = packageSprite.GetComponent<SpriteGridFill>();
        }

        void OnEnable()
        {
            _dotsLeft = Mathf.Max(1, totalClicks);
            _reservedDots = 0;
            _depleted = false;
            clicksLeftDebug = _dotsLeft;
            RefreshVisuals(); // đổi sprite/màu gói theo fixedColorId + cập nhật fill
            if (!s_all.Contains(this)) s_all.Add(this);
        }

        /// <summary>
        /// TINT màu gói theo <see cref="fixedColorId"/> (GIỮ NGUYÊN sprite gói):
        /// lấy màu từ FruitDatabase, fallback palette. fixedColorId = -1 (random) -> trắng.
        /// </summary>
        public void RefreshVisuals()
        {
            if (packageSprite == null) packageSprite = GetComponent<SpriteRenderer>();
            if (packageSprite == null) return;

            SpriteRenderer defaultDotRenderer = dotPrefab != null
                ? dotPrefab.GetComponent<SpriteRenderer>()
                : null;
            if (defaultDotRenderer != null && defaultDotRenderer.sprite != null)
                packageSprite.sprite = defaultDotRenderer.sprite;

            FruitData fruit = (fixedColorId >= 0 && fruitDatabase != null)
                ? fruitDatabase.GetById(fixedColorId) : null;

            if (fruit != null)
                packageSprite.color = fruit.color;
            else if (fixedColorId >= 0 && palette != null && fixedColorId < palette.Length)
                packageSprite.color = palette[fixedColorId];
            else if (fixedColorId < 0)
                packageSprite.color = Color.white;

            UpdateFillVisual();
        }

        void OnDisable()
        {
            s_all.Remove(this);
        }

        void Update()
        {
            // Popup đang đè -> không nhận click gameplay (input là polling, UI không chặn được).
            if (GameplayPause.IsPaused) return;

            // Cột quản lý -> không tự xử lý click.
            if (managedExternally) return;

            // Chỉ kích hoạt ở frame NHẤN XUỐNG (edge), tránh spawn liên tục khi giữ.
            // Xử lý TẬP TRUNG: bất kỳ instance nào phát hiện edge cũng gọi handler chung,
            // handler tự bảo đảm chỉ chạy 1 lần/frame.
            if (PointerInput.PressedThisFrame()) HandleGlobalClick();
        }

        /// <summary>
        /// Xử lý 1 cú click (1 lần/frame cho mọi spawner): tìm spawner TRÊN CÙNG mà con trỏ
        /// nằm trong bounds rồi chỉ gọi DoClick() cho nó. "Trên cùng" = sortingLayer cao hơn,
        /// rồi sortingOrder cao hơn, rồi z nhỏ hơn (gần camera).
        /// </summary>
        static void HandleGlobalClick()
        {
            if (s_lastClickFrame == Time.frameCount) return; // đã xử lý frame này rồi
            s_lastClickFrame = Time.frameCount;

            // UI đang đè lên -> không cho click xuyên qua xuống gói.
            if (UIPointerGuard.IsPointerOverUI()) return;
            if (!PointerInput.TryGetPosition(out Vector2 pointerPos)) return;
            Vector3 screenPos = pointerPos;

            ModelDotSpawner best = null;
            for (int i = 0; i < s_all.Count; i++)
            {
                var s = s_all[i];
                if (s == null || s._depleted || s.managedExternally || !s.isActiveAndEnabled) continue;
                if (!s.HitTest(screenPos)) continue;
                if (best == null || s.IsAbove(best)) best = s;
            }

            if (best != null) best.DoClick();
        }

        /// <summary>Con trỏ (screen) có nằm trong bounds sprite của gói này không?</summary>
        public bool HitTest(Vector3 screenPos)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null || packageSprite == null) return false;

            // Quy đổi vị trí chuột về mặt phẳng z của sprite, rồi kiểm tra bounds (không cần collider).
            Ray ray = cam.ScreenPointToRay(screenPos);
            float zPlane = packageSprite.transform.position.z;

            Vector3 world;
            if (Mathf.Abs(ray.direction.z) < 1e-6f) world = ray.origin;
            else world = ray.origin + ray.direction * ((zPlane - ray.origin.z) / ray.direction.z);

            Bounds b = packageSprite.bounds;
            return world.x >= b.min.x && world.x <= b.max.x && world.y >= b.min.y && world.y <= b.max.y;
        }

        /// <summary>Gói này có nằm TRÊN gói 'other' theo thứ tự vẽ 2D không?</summary>
        bool IsAbove(ModelDotSpawner other)
        {
            if (other == null || other.packageSprite == null) return true;
            if (packageSprite == null) return false;

            int la = SortingLayer.GetLayerValueFromID(packageSprite.sortingLayerID);
            int lb = SortingLayer.GetLayerValueFromID(other.packageSprite.sortingLayerID);
            if (la != lb) return la > lb;

            if (packageSprite.sortingOrder != other.packageSprite.sortingOrder)
                return packageSprite.sortingOrder > other.packageSprite.sortingOrder;

            // Hoà sorting -> z nhỏ hơn (gần camera hơn) coi như ở trên.
            return packageSprite.transform.position.z < other.packageSprite.transform.position.z;
        }

        /// <summary>Spawn một loạt dot từ các ô grid đang đầy.</summary>
        public void DoClick()
        {
            if (_depleted) return;

            GamePlayManager gamePlay = GamePlayManager.Instance;
            if (gamePlay != null && !gamePlay.CanUseMove)
                return;

            int available = Mathf.Max(0, _dotsLeft - _reservedDots);
            int count = Mathf.Min(Mathf.Max(1, spawnCount), available);
            if (count <= 0) return;

            // Phản hồi xúc giác: punch scale gói khi click.
            if (clickPunch > 0f)
            {
                Transform vt = packageSprite != null ? packageSprite.transform : transform;
                vt.DOKill(true); // hoàn tất punch dở để không lệch scale gốc
                vt.DOPunchScale(Vector3.one * clickPunch, clickPunchDuration, 10, 0.8f);
            }

            _reservedDots += count;
            if (spawnInterval <= 0f)
            {
                for (int i = 0; i < count; i++) SpawnOneAndConsume();
            }
            else
            {
                StartCoroutine(SpawnAndConsumeRoutine(count));
            }

            if (gamePlay != null) gamePlay.RecordInteraction();
        }

        void Deplete()
        {
            _depleted = true;
            onDepleted?.Invoke();

            // Fade out rồi ẩn.
            if (packageSprite != null && fadeOutDuration > 0f)
            {
                packageSprite.DOFade(0f, fadeOutDuration)
                             .OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        void UpdateFillVisual()
        {
            UpdateCountText();

            if (gridFill == null && packageSprite != null)
                gridFill = packageSprite.GetComponent<SpriteGridFill>();
            if (gridFill == null) return;

            // Lưới vuông cân bằng (rows = columns), không cần khớp đúng số dot.
            gridFill.SetBalancedGrid(totalClicks);
            gridFill.CellGap = cellGap;
            // Edit mode: _dotsLeft (biến runtime) = 0 -> luôn hiển thị gói ĐẦY để preview.
            gridFill.FillAmount = Application.isPlaying
                ? Mathf.Clamp01(_dotsLeft / (float)Mathf.Max(1, totalClicks))
                : 1f;
        }

        int _lastShownCount = int.MinValue;

        void UpdateCountText()
        {
            EnsureCountText();
            if (countText == null) return;

            // Edit mode: hiển thị tổng gói để preview (giống fill luôn đầy).
            int shown = Application.isPlaying ? _dotsLeft : Mathf.Max(1, totalClicks);
            countText.text = shown.ToString();

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
            if (textRenderer != null && packageSprite != null)
            {
                textRenderer.sortingLayerID = packageSprite.sortingLayerID;
                textRenderer.sortingOrder = packageSprite.sortingOrder + 20;
            }

            countText = tmp;
        }

        /// <summary>Sinh loạt dot (1 lần click).</summary>
        public void Spawn()
        {
            if (spawnInterval <= 0f)
            {
                for (int i = 0; i < spawnCount; i++) SpawnOne();
            }
            else
            {
                StartCoroutine(SpawnRoutine());
            }
        }

        IEnumerator SpawnAndConsumeRoutine(int count)
        {
            var wait = new WaitForSeconds(spawnInterval);
            for (int i = 0; i < count; i++)
            {
                SpawnOneAndConsume();
                if (i + 1 < count) yield return wait;
            }
        }

        void SpawnOneAndConsume()
        {
            bool spawned = SpawnOne();
            _reservedDots = Mathf.Max(0, _reservedDots - 1);

            if (spawned)
            {
                _dotsLeft = Mathf.Max(0, _dotsLeft - 1);
            }
            clicksLeftDebug = _dotsLeft;
            UpdateFillVisual();

            // Chỉ ẩn sau khi dot cuối của mọi loạt đang chờ đã thực sự được sinh.
            if (_dotsLeft <= 0 && _reservedDots <= 0) Deplete();
        }


        IEnumerator SpawnRoutine()
        {
            var wait = new WaitForSeconds(spawnInterval);
            for (int i = 0; i < spawnCount; i++)
            {
                // Popup đè lên giữa loạt spawn -> đứng chờ, không sinh dot trong lúc pause.
                while (GameplayPause.IsPaused) yield return null;

                SpawnOne();
                yield return wait;
            }
        }

        bool SpawnOne()
        {
            if (dotPrefab == null) { Debug.LogError("[ModelDotSpawner] Chưa gán dotPrefab."); return false; }

            FallingPixelManager fm = fallingManager != null ? fallingManager : FallingPixelManager.Instance;
            if (fm == null) { Debug.LogError("[ModelDotSpawner] Không tìm thấy FallingPixelManager."); return false; }

            Vector3 baseP = spawnOrigin != null ? spawnOrigin.position : transform.position;
            Vector2 r = Random.insideUnitCircle * spawnSpread;
            Vector3 pos = baseP + new Vector3(r.x, r.y, 0f);

            if (!TryResolveSpawnAppearance(out int colorId, out Color c, out Sprite spr))
                return false;

            Dot d = Instantiate(dotPrefab, pos, Quaternion.identity);
            d.transform.localScale = Vector3.one * dotScale;
            d.Init(colorId, c, dotHP, new Vector2Int(-1, -1), spr);

            fm.LaunchDot(d, pos, launchDirection, launchSpeed, launchSpread);

            // Pop scale để dot xuất hiện mềm thay vì bật ra đột ngột (thêm SAU LaunchDot
            // vì LaunchDot có DOKill transform).
            d.transform.localScale = Vector3.zero;
            d.transform.DOScale(Vector3.one * dotScale, 0.15f)
             .SetEase(Ease.OutBack)
             .SetLink(d.gameObject);
            return true;
        }

        public bool TryResolveSpawnAppearance(
            out int resolvedColorId,
            out Color resolvedColor,
            out Sprite resolvedSprite)
        {
            resolvedColorId = 0;
            resolvedColor = Color.white;
            resolvedSprite = null;

            FruitData fruit = null;
            if (fixedColorId >= 0)
            {
                fruit = fruitDatabase != null ? fruitDatabase.GetById(fixedColorId) : null;
                if (fruit != null)
                {
                    resolvedColorId = fruit.colorId;
                    resolvedColor = fruit.color;
                    return true;
                }

                int paletteLength = palette != null ? palette.Length : 0;
                if (paletteLength <= 0) return false;
                resolvedColorId = Mathf.Clamp(fixedColorId, 0, paletteLength - 1);
                resolvedColor = palette[resolvedColorId];
                return true;
            }

            if (fruitDatabase != null && fruitDatabase.fruits != null)
            {
                int validCount = 0;
                for (int i = 0; i < fruitDatabase.fruits.Length; i++)
                    if (fruitDatabase.fruits[i] != null) validCount++;

                if (validCount > 0)
                {
                    int pick = Random.Range(0, validCount);
                    for (int i = 0; i < fruitDatabase.fruits.Length; i++)
                    {
                        FruitData candidate = fruitDatabase.fruits[i];
                        if (candidate == null) continue;
                        if (pick-- > 0) continue;
                        resolvedColorId = candidate.colorId;
                        resolvedColor = candidate.color;
                        return true;
                    }
                }
            }

            int fallbackLength = palette != null ? palette.Length : 0;
            if (fallbackLength <= 0) return false;
            resolvedColorId = Random.Range(0, fallbackLength);
            resolvedColor = palette[resolvedColorId];
            return true;
        }

        public static int CountPendingDotsForColor(int colorId)
        {
            int count = 0;
            for (int i = 0; i < s_all.Count; i++)
            {
                ModelDotSpawner spawner = s_all[i];
                if (spawner == null || !spawner.isActiveAndEnabled || spawner._depleted)
                    continue;
                if (spawner._reservedDots <= 0)
                    continue;
                if (spawner.TryGetFixedSpawnColorId(out int fixedId) && fixedId == colorId)
                    count += spawner._reservedDots;
                else if (spawner.fixedColorId < 0 && spawner.CanRandomlySpawnColor(colorId))
                    count += spawner._reservedDots;
            }
            return count;
        }

        bool TryGetFixedSpawnColorId(out int resolvedColorId)
        {
            resolvedColorId = 0;
            if (fixedColorId < 0) return false;

            FruitData fruit = fruitDatabase != null ? fruitDatabase.GetById(fixedColorId) : null;
            if (fruit != null)
            {
                resolvedColorId = fruit.colorId;
                return true;
            }

            int paletteLength = palette != null ? palette.Length : 0;
            if (paletteLength <= 0) return false;
            resolvedColorId = Mathf.Clamp(fixedColorId, 0, paletteLength - 1);
            return true;
        }

        bool CanRandomlySpawnColor(int colorId)
        {
            if (fruitDatabase != null && fruitDatabase.fruits != null)
            {
                for (int i = 0; i < fruitDatabase.fruits.Length; i++)
                {
                    FruitData fruit = fruitDatabase.fruits[i];
                    if (fruit != null && fruit.colorId == colorId)
                        return true;
                }
            }

            return palette != null && colorId >= 0 && colorId < palette.Length;
        }

        void OnValidate()
        {
            totalClicks = Mathf.Max(1, totalClicks);
            cellGap = Mathf.Clamp(cellGap, 0f, 0.45f);
            launchSpeed = Mathf.Max(0.1f, launchSpeed);
            launchSpread = Mathf.Clamp(launchSpread, 0f, 45f);
            if (packageSprite == null) packageSprite = GetComponent<SpriteRenderer>();
            if (packageSprite != null && gridFill == null) gridFill = packageSprite.GetComponent<SpriteGridFill>();
            RefreshVisuals();
        }
    }
}
