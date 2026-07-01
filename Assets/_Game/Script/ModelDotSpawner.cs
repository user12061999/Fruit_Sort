using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem; // Project dùng Input System (New)
using DG.Tweening;

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
        [Tooltip("Để tra sprite theo colorId. Gán -> dot đổi sprite thành art của màu tương ứng.")]
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
        [Tooltip("Số ô mỗi hàng; số hàng tự tính từ Dots For Full Sprite.")]
        [Min(1)] public int gridColumns = 16;
        [Tooltip("Khoảng trong suốt giữa các ô.")]
        [Range(0f, 0.45f)] public float cellGap = 0.02f;

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

        [Header("Vào băng chuyền")]
        [Tooltip("Bật: ép tất cả dot vào belt tại 1 progress cố định. Tắt: dùng spawnEntryProgress của FallingPixelManager + lateral random.")]
        public bool overrideEntry = false;
        [Range(0f, 1f)] public float entryProgress = 0f;

        [Header("Phóng theo hướng cố định")]
        [Tooltip("Bật: phóng dot theo hướng launchDirection, tự va chạm băng chuyền. " +
                 "Tắt (mặc định): bay thẳng tới 1 điểm cố định trên băng chuyền.")]
        public bool useDirectionalLaunch = false;
        [Tooltip("Hướng phóng (sẽ normalize). Ví dụ: (0,-1) = thẳng xuống, (1,-1) = xuống phải.")]
        public Vector2 launchDirection = Vector2.down;
        [Tooltip("Tốc độ phóng ban đầu (world unit/giây).")]
        [Min(0.1f)] public float launchSpeed = 10f;
        [Tooltip("Độ tản hướng ngẫu nhiên (±độ). 0 = tất cả dot đi thẳng một hướng.")]
        [Range(0f, 45f)] public float launchSpread = 3f;

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
        bool _wasPressed;

        /// <summary>Spawner đã rỗng gói chưa.</summary>
        public bool IsDepleted => _depleted;

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
            UpdateFillVisual();
            if (!s_all.Contains(this)) s_all.Add(this);
        }

        void OnDisable()
        {
            s_all.Remove(this);
        }

        void Update()
        {
            // Cột quản lý -> không tự xử lý click.
            if (managedExternally) return;
            if (Mouse.current == null) return;

            bool pressed = Mouse.current.leftButton.isPressed;
            // Chỉ kích hoạt ở frame NHẤN XUỐNG (edge), tránh spawn liên tục khi giữ chuột.
            // Xử lý TẬP TRUNG: bất kỳ instance nào phát hiện edge cũng gọi handler chung,
            // handler tự bảo đảm chỉ chạy 1 lần/frame.
            if (pressed && !_wasPressed) HandleGlobalClick();
            _wasPressed = pressed;
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

            if (Mouse.current == null) return;
            Vector3 screenPos = Mouse.current.position.ReadValue();

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

            int available = Mathf.Max(0, _dotsLeft - _reservedDots);
            int count = Mathf.Min(Mathf.Max(1, spawnCount), available);
            if (count <= 0) return;

            _reservedDots += count;
            if (spawnInterval <= 0f)
            {
                for (int i = 0; i < count; i++) SpawnOneAndConsume();
            }
            else
            {
                StartCoroutine(SpawnAndConsumeRoutine(count));
            }
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
            if (gridFill == null && packageSprite != null)
                gridFill = packageSprite.GetComponent<SpriteGridFill>();
            if (gridFill == null) return;

            int columns = Mathf.Max(1, gridColumns);
            int rows = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1, totalClicks) / (float)columns));
            gridFill.SetGrid(columns, rows);
            gridFill.CellGap = cellGap;
            gridFill.FillAmount = Mathf.Clamp01(_dotsLeft / (float)Mathf.Max(1, totalClicks));
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

            if (useDirectionalLaunch)
            {
                Vector2 dir = launchDirection.sqrMagnitude > 0.001f
                    ? launchDirection.normalized
                    : Vector2.down;

                // Xoay hướng ngẫu nhiên trong ±launchSpread độ.
                float spreadRad = launchSpread * Mathf.Deg2Rad;
                float angle = Random.Range(-spreadRad, spreadRad);
                float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                Vector2 vel = new Vector2(
                    dir.x * cos - dir.y * sin,
                    dir.x * sin + dir.y * cos
                ) * launchSpeed;

                fm.AddDotLaunched(d, vel);
            }
            else
            {
                float t = overrideEntry ? entryProgress : float.NaN;
                fm.AddDotApproaching(d, t);
            }
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
                    resolvedSprite = fruit.dotSprite;
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
                        resolvedSprite = candidate.dotSprite;
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

        void OnValidate()
        {
            totalClicks = Mathf.Max(1, totalClicks);
            gridColumns = Mathf.Max(1, gridColumns);
            cellGap = Mathf.Clamp(cellGap, 0f, 0.45f);
            if (packageSprite == null) packageSprite = GetComponent<SpriteRenderer>();
            if (packageSprite != null && gridFill == null) gridFill = packageSprite.GetComponent<SpriteGridFill>();
        }
    }
}
