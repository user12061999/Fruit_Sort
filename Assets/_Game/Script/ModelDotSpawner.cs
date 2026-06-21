using System.Collections;
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
        MaterialPropertyBlock _mpb;
        static readonly int FillAmountID = Shader.PropertyToID("_FillAmount");

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
        }

        void Update()
        {
            if (_depleted || Mouse.current == null) return;

            bool pressed = Mouse.current.leftButton.isPressed;
            // Chỉ kích hoạt ở frame NHẤN XUỐNG (edge), tránh spawn liên tục khi giữ chuột.
            if (pressed && !_wasPressed) TryClick();
            _wasPressed = pressed;
        }

        void TryClick()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null || packageSprite == null) return;

            // Quy đổi vị trí chuột về mặt phẳng z của sprite, rồi kiểm tra bounds (không cần collider).
            Vector3 mp = Mouse.current.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(mp);
            float zPlane = packageSprite.transform.position.z;

            Vector3 world;
            if (Mathf.Abs(ray.direction.z) < 1e-6f) world = ray.origin;
            else world = ray.origin + ray.direction * ((zPlane - ray.origin.z) / ray.direction.z);

            Bounds b = packageSprite.bounds;
            if (world.x >= b.min.x && world.x <= b.max.x && world.y >= b.min.y && world.y <= b.max.y)
                DoClick();
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

            float t = overrideEntry ? entryProgress : float.NaN;
            fm.AddDotApproaching(d, t);
        }
    }
}
