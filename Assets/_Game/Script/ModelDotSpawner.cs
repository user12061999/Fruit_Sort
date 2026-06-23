using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem; // Project dùng Input System (New)
using DG.Tweening;

namespace FruitSort
{
    /// <summary>
    /// "GÓI" (package) hiển thị bằng 1 SpriteRenderer. Mỗi lần click vào sprite:
    /// - Sinh ra spawnCount dot bay vào băng chuyền (như cũ).
    /// - Sprite VƠI đi 1 phần (qua shader FruitSort/SpriteFill, _FillAmount giảm dần 1 -> 0).
    /// Click đủ totalClicks lần -> gói rỗng -> gọi onDepleted (UnityEvent) -> fade out -> ẩn.
    /// </summary>
    public class ModelDotSpawner : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("Prefab dot (giống dotPrefab của PixelGridManager).")]
        public Dot dotPrefab;
        [Tooltip("Để trống = tự dùng FallingPixelManager.Instance.")]
        public FallingPixelManager fallingManager;
        [Tooltip("Camera dùng để quy đổi vị trí click. Để trống = Camera.main.")]
        public Camera cam;
        [Tooltip("Sprite của GÓI (sẽ vơi dần). Để trống = tự lấy SpriteRenderer trên object này.")]
        public SpriteRenderer packageSprite;

        [Header("Gói (package)")]
        [Tooltip("Số lần click để gói rỗng hẳn.")]
        [Min(1)] public int totalClicks = 5;
        [Tooltip("Số click còn lại (chỉ đọc tham khảo lúc play).")]
        public int clicksLeftDebug;

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
        int _clicksLeft;
        bool _depleted;
        bool _wasPressed;

        /// <summary>Spawner đã rỗng gói chưa.</summary>
        public bool IsDepleted => _depleted;

        /// <summary>Bật khi spawner được 1 ModelDotSpawnerColumn quản lý: nó sẽ KHÔNG tự xử lý
        /// click nữa (cột sẽ gọi DoClick thay).</summary>
        [System.NonSerialized] public bool managedExternally;
        MaterialPropertyBlock _mpb;
        static readonly int FillAmountID = Shader.PropertyToID("_FillAmount");

        // Mọi spawner đang bật. Click được xử lý TẬP TRUNG (1 lần/frame) để khi nhiều
        // spawner chồng nhau, CHỈ cái trên cùng (theo sorting order của sprite) spawn.
        static readonly List<ModelDotSpawner> s_all = new List<ModelDotSpawner>();
        static int s_lastClickFrame = -1;

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            if (packageSprite == null) packageSprite = GetComponent<SpriteRenderer>();
            if (fallingManager == null) fallingManager = FallingPixelManager.Instance;
        }

        void OnEnable()
        {
            _clicksLeft = Mathf.Max(1, totalClicks);
            _depleted = false;
            clicksLeftDebug = _clicksLeft;
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

        /// <summary>1 lần click vào gói: spawn dot + vơi sprite. Có thể gọi từ UI button nếu muốn.</summary>
        public void DoClick()
        {
            if (_depleted) return;

            Spawn();

            _clicksLeft = Mathf.Max(0, _clicksLeft - 1);
            clicksLeftDebug = _clicksLeft;
            UpdateFillVisual();

            if (_clicksLeft <= 0) Deplete();
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
            if (packageSprite == null) return;
            float fill = totalClicks > 0 ? Mathf.Clamp01(_clicksLeft / (float)totalClicks) : 0f;

            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            packageSprite.GetPropertyBlock(_mpb);
            _mpb.SetFloat(FillAmountID, fill);
            packageSprite.SetPropertyBlock(_mpb);
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

        IEnumerator SpawnRoutine()
        {
            var wait = new WaitForSeconds(spawnInterval);
            for (int i = 0; i < spawnCount; i++)
            {
                SpawnOne();
                yield return wait;
            }
        }

        void SpawnOne()
        {
            if (dotPrefab == null) { Debug.LogError("[ModelDotSpawner] Chưa gán dotPrefab."); return; }

            FallingPixelManager fm = fallingManager != null ? fallingManager : FallingPixelManager.Instance;
            if (fm == null) { Debug.LogError("[ModelDotSpawner] Không tìm thấy FallingPixelManager."); return; }

            Vector3 baseP = spawnOrigin != null ? spawnOrigin.position : transform.position;
            Vector2 r = Random.insideUnitCircle * spawnSpread;
            Vector3 pos = baseP + new Vector3(r.x, r.y, 0f);

            // Chọn màu.
            int paletteLen = (palette != null && palette.Length > 0) ? palette.Length : 1;
            int colorId = (fixedColorId >= 0)
                ? Mathf.Clamp(fixedColorId, 0, paletteLen - 1)
                : Random.Range(0, paletteLen);
            Color c = (palette != null && palette.Length > 0) ? palette[colorId] : Color.white;

            Dot d = Instantiate(dotPrefab, pos, Quaternion.identity);
            d.transform.localScale = Vector3.one * dotScale;
            d.Init(colorId, c, dotHP, new Vector2Int(-1, -1));

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
        }
    }
}
