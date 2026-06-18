using UnityEngine;
using UnityEngine.InputSystem; // Project dùng Input System (New) -> activeInputHandler = 1

namespace FruitSort
{
    /// <summary>
    /// Bắn đạn hitscan (tức thời) từ dưới lên. Mỗi phát trúng dot -> gọi DamageDot (giảm HP).
    /// Hỗ trợ bắn thẳng lên hoặc nhắm theo chuột.
    /// </summary>
    public class Shooter : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("Điểm xuất phát đạn. Để trống = dùng chính transform này.")]
        public Transform muzzle;
        public PixelGridManager gridManager;
        public Camera cam;

        [Header("Bắn")]
        public int damage = 1;
        [Tooltip("Số phát mỗi giây.")]
        public float fireRate = 8f;
        public float maxRange = 30f;
        [Tooltip("Bán kính phụ khi dò trúng (nới rộng độ trúng).")]
        public float hitRadius = 0.1f;
        [Tooltip("Bật: nhắm theo con trỏ chuột. Tắt: luôn bắn thẳng lên.")]
        public bool aimAtMouse = true;

        [Header("Tracer (tuỳ chọn)")]
        public LineRenderer tracer;
        public float tracerTime = 0.05f;
        public Color tracerColor = Color.white;

        float _cooldown;
        float _tracerTimer;

        void Awake()
        {
            if (muzzle == null) muzzle = transform;
            if (cam == null) cam = Camera.main;
            if (gridManager == null) gridManager = FindFirstObjectByType<PixelGridManager>();
            if (tracer != null) tracer.positionCount = 2;
        }

        void Update()
        {
            _cooldown -= Time.deltaTime;

            bool firing = Mouse.current != null && Mouse.current.leftButton.isPressed;
            if (firing && _cooldown <= 0f)
            {
                Fire();
                _cooldown = 1f / Mathf.Max(0.01f, fireRate);
            }

            // Tắt tracer sau 1 khoảng ngắn.
            if (tracer != null && tracer.enabled)
            {
                _tracerTimer -= Time.deltaTime;
                if (_tracerTimer <= 0f) tracer.enabled = false;
            }
        }

        void Fire()
        {
            Vector2 origin = muzzle.position;
            Vector2 dir = Vector2.up;

            if (aimAtMouse && cam != null && Mouse.current != null)
            {
                Vector3 mp = Mouse.current.position.ReadValue();
                mp.z = -cam.transform.position.z; // khoảng cách tới mặt phẳng z=0
                Vector3 world = cam.ScreenToWorldPoint(mp);
                Vector2 d = (Vector2)world - origin;
                if (d.sqrMagnitude > 1e-4f) dir = d.normalized;
            }

            Vector2 hitPoint = origin + dir * maxRange;

            if (gridManager != null)
            {
                Dot hit = gridManager.RaycastDot(origin, dir, maxRange, hitRadius);
                if (hit != null)
                {
                    hitPoint = hit.transform.position;
                    gridManager.DamageDot(hit, damage);
                }
            }

            ShowTracer(origin, hitPoint);
        }

        void ShowTracer(Vector2 a, Vector2 b)
        {
            if (tracer == null) return;
            tracer.enabled = true;
            tracer.startColor = tracer.endColor = tracerColor;
            tracer.SetPosition(0, a);
            tracer.SetPosition(1, b);
            _tracerTimer = tracerTime;
        }
    }
}
