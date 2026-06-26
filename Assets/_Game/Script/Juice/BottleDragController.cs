using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

namespace FruitSort
{
    /// <summary>
    /// Điều khiển kéo-thả bình để pha màu. Đặt 1 cái trong scene.
    /// - Nhấn chuột lên 1 bình => nhấc bình theo con trỏ.
    /// - Thả lên BÌNH khác  => đổ (pha màu) bình đang cầm vào bình đó; bình cầm thành rỗng.
    /// - Thả lên ĐƠN HÀNG   => giao bình nếu khớp màu.
    /// - Thả ra chỗ trống   => bình quay về ô của nó.
    /// Mỗi bình cần có Collider2D (2D) để nhận biết. Dùng new Input System (Mouse.current).
    /// </summary>
    public class BottleDragController : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("Camera để đổi toạ độ chuột. Để trống = Camera.main.")]
        public Camera cam;

        [Header("Kéo")]
        [Tooltip("Bình được nhấc cao thêm bao nhiêu khi cầm (theo trục Z để vẽ đè).")]
        public float dragZ = -1f;
        [Tooltip("Phóng to bình khi đang cầm.")]
        public float dragScale = 1.15f;

        JuiceBottle _dragging;
        Vector3 _dragScaleBackup;
        int _sortBackup;
        SpriteRenderer _draggingSr;
        bool _wasPressed;

        readonly Collider2D[] _hits = new Collider2D[8];

        void Awake()
        {
            if (cam == null) cam = Camera.main;
        }

        void Update()
        {
            if (Mouse.current == null) return;
            bool pressed = Mouse.current.leftButton.isPressed;
            Vector3 world = ScreenToWorld(Mouse.current.position.ReadValue());

            if (pressed && !_wasPressed) TryBeginDrag(world);
            else if (pressed && _dragging != null) DragTo(world);
            else if (!pressed && _wasPressed && _dragging != null) EndDrag(world);

            _wasPressed = pressed;
        }

        Vector3 ScreenToWorld(Vector2 screen)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return Vector3.zero;
            Vector3 w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            w.z = 0f;
            return w;
        }

        void TryBeginDrag(Vector3 world)
        {
            JuiceBottle picked = FindBottleAt(world, null);
            if (picked == null || picked.IsEmpty || picked.IsBusy) return; // chỉ cầm bình có nước, không bận

            _dragging = picked;
            _dragScaleBackup = picked.transform.localScale;
            picked.transform.DOKill();
            picked.transform.localScale = _dragScaleBackup * dragScale;

            _draggingSr = picked.liquid != null ? picked.liquid : picked.GetComponentInChildren<SpriteRenderer>();
            if (_draggingSr != null) { _sortBackup = _draggingSr.sortingOrder; _draggingSr.sortingOrder = _sortBackup + 100; }
        }

        void DragTo(Vector3 world)
        {
            _dragging.transform.position = new Vector3(world.x, world.y, dragZ);
        }

        void EndDrag(Vector3 world)
        {
            JuiceBottle held = _dragging;
            _dragging = null;

            // Khôi phục scale / sorting.
            held.transform.localScale = _dragScaleBackup;
            if (_draggingSr != null) _draggingSr.sortingOrder = _sortBackup;

            // Thả lên bình khác => pha (có hiệu ứng rót nếu có JuicePourFX).
            JuiceBottle target = FindBottleAt(world, held);
            if (target != null && !target.IsBusy)
            {
                if (JuicePourFX.Instance != null)
                {
                    JuicePourFX.Instance.Pour(held, target);
                }
                else
                {
                    target.ReceiveFrom(held);
                    held.ReturnHome();
                }
                return;
            }

            // Thả lên đơn hàng => giao.
            JuiceOrder order = FindOrderAt(world);
            if (order != null && order.TryDeliver(held))
            {
                held.ReturnHome();
                return;
            }

            // Không có gì => quay về.
            held.ReturnHome();
        }

        JuiceBottle FindBottleAt(Vector3 world, JuiceBottle exclude)
        {
            int n = Physics2D.OverlapPointNonAlloc(world, _hits);
            JuiceBottle best = null;
            for (int i = 0; i < n; i++)
            {
                var b = _hits[i].GetComponentInParent<JuiceBottle>();
                if (b == null || b == exclude) continue;
                best = b; // lấy cái đầu tiên hợp lệ
                break;
            }
            return best;
        }

        JuiceOrder FindOrderAt(Vector3 world)
        {
            int n = Physics2D.OverlapPointNonAlloc(world, _hits);
            for (int i = 0; i < n; i++)
            {
                var o = _hits[i].GetComponentInParent<JuiceOrder>();
                if (o != null) return o;
            }
            return null;
        }
    }
}
