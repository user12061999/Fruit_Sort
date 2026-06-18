using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Project dùng Input System (New)

namespace FruitSort
{
    /// <summary>
    /// Gắn lên 1 MODEL 3D. Khi click (chuột trái) trúng model -> sinh ra N dot có màu
    /// tại vị trí model, các dot bay THẲNG vào băng chuyền (spline) rồi chạy như bình thường.
    /// Số lượng dot mỗi lần spawn cấu hình qua <see cref="spawnCount"/>.
    /// </summary>
    public class ModelDotSpawner : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("Prefab dot (giống dotPrefab của PixelGridManager).")]
        public Dot dotPrefab;
        [Tooltip("Để trống = tự dùng FallingPixelManager.Instance.")]
        public FallingPixelManager fallingManager;
        [Tooltip("Camera dùng để raycast click. Để trống = Camera.main.")]
        public Camera cam;
        [Tooltip("Collider của model để nhận click. Để trống = lấy Collider trên chính object này.")]
        public Collider modelCollider;

        [Header("Cấu hình spawn")]
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

        bool _wasPressed;

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            if (modelCollider == null) modelCollider = GetComponent<Collider>();
            if (fallingManager == null) fallingManager = FallingPixelManager.Instance;
        }

        void Update()
        {
            if (Mouse.current == null) return;

            bool pressed = Mouse.current.leftButton.isPressed;
            // Chỉ kích hoạt ở frame NHẤN XUỐNG (edge), tránh spawn liên tục khi giữ chuột.
            if (pressed && !_wasPressed) TryClick();
            _wasPressed = pressed;
        }

        void TryClick()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            Vector3 mp = Mouse.current.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(mp);

            // Có collider riêng -> chỉ test collider đó (chính xác, không vướng object khác).
            if (modelCollider != null)
            {
                if (modelCollider.Raycast(ray, out _, 1000f)) Spawn();
            }
            else if (Physics.Raycast(ray, out RaycastHit hit, 1000f) && hit.transform == transform)
            {
                Spawn();
            }
        }

        /// <summary>Sinh loạt dot (có thể gọi từ code/UI button khác nếu muốn).</summary>
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
