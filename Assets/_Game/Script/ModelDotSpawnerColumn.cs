using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // Project dùng Input System (New)

namespace FruitSort
{
    /// <summary>
    /// Quản lý 1 CỘT các <see cref="ModelDotSpawner"/> là CON của GameObject này.
    /// Tại một thời điểm chỉ 1 spawner "active" (theo THỨ TỰ CON trong hierarchy: con đầu trước).
    /// Click vào cột -> chỉ spawner đang active mới DoClick(). Khi active rỗng gói (depleted),
    /// cột tự chuyển sang con kế tiếp còn dùng được.
    ///
    /// Cách này KHÔNG phụ thuộc sorting order -> tránh lỗi "spawner sorting thấp không click được".
    /// </summary>
    public class ModelDotSpawnerColumn : MonoBehaviour
    {
        [Tooltip("Camera để quy đổi vị trí click. Để trống = Camera.main (và gán xuống cho spawner con nếu chúng trống).")]
        public Camera cam;

        [Tooltip("BẬT: click trúng box của BẤT KỲ spawner nào trong cột đều kích hoạt cái đang active " +
                 "(tiện khi các box chồng nhau). TẮT: phải click đúng box của spawner active.")]
        public bool clickAnywhereInColumn = true;

        [Tooltip("Chỉ đọc lúc play: index spawner đang active trong cột.")]
        public int currentIndexDebug;

        readonly List<ModelDotSpawner> _members = new List<ModelDotSpawner>();
        bool _wasPressed;

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            Collect();
        }

        void OnEnable()
        {
            if (cam == null) cam = Camera.main;
            Collect();
        }

        /// <summary>Thu thập các spawner con (theo thứ tự cây) và đánh dấu chúng do cột quản lý.</summary>
        public void Collect()
        {
            _members.Clear();
            // includeInactive = true, GetComponentsInChildren trả về theo thứ tự duyệt cây (con đầu trước).
            GetComponentsInChildren(true, _members);

            for (int i = 0; i < _members.Count; i++)
            {
                var m = _members[i];
                m.managedExternally = true;            // chặn spawner tự xử lý click
                if (m.cam == null) m.cam = cam;
            }
        }

        void Update()
        {
            if (Mouse.current == null) return;

            bool pressed = Mouse.current.leftButton.isPressed;
            // Edge nhấn xuống mới tính (tránh spawn liên tục khi giữ chuột).
            if (pressed && !_wasPressed) TryClick();
            _wasPressed = pressed;

            currentIndexDebug = ActiveIndex();
        }

        void TryClick()
        {
            int idx = ActiveIndex();
            if (idx < 0) return;
            ModelDotSpawner active = _members[idx];

            Vector3 sp = Mouse.current.position.ReadValue();

            bool hit;
            if (clickAnywhereInColumn)
            {
                hit = false;
                for (int i = 0; i < _members.Count; i++)
                {
                    var m = _members[i];
                    if (m != null && m.isActiveAndEnabled && !m.IsDepleted && m.HitTest(sp)) { hit = true; break; }
                }
            }
            else
            {
                hit = active.HitTest(sp);
            }

            if (hit) active.DoClick();
        }

        /// <summary>
        /// Index spawner ĐANG active = thành viên ĐẦU TIÊN (theo thứ tự con) còn dùng được
        /// (chưa depleted, đang bật). Tính ĐỘNG mỗi frame -> tự hồi phục, không kẹt như con trỏ
        /// một chiều. Trả về -1 nếu cả cột đã hết.
        /// </summary>
        int ActiveIndex()
        {
            for (int i = 0; i < _members.Count; i++)
            {
                var m = _members[i];
                if (m != null && m.isActiveAndEnabled && !m.IsDepleted) return i;
            }
            return -1;
        }
    }
}
