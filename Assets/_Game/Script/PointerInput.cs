using UnityEngine;
using UnityEngine.InputSystem; // Project dùng Input System (New)

namespace FruitSort
{
    /// <summary>
    /// Gom input con trỏ (chuột + cảm ứng) qua <c>Pointer.current</c> của Input System.
    /// Mouse.current là NULL trên thiết bị chỉ có touch (Android/iOS) -> mọi script
    /// gameplay phải đọc qua lớp này thay vì Mouse.current để chơi được trên mobile.
    /// </summary>
    public static class PointerInput
    {
        /// <summary>Con trỏ (chuột trái / touch chính) vừa NHẤN XUỐNG trong frame này?</summary>
        public static bool PressedThisFrame()
        {
            Pointer pointer = Pointer.current;
            return pointer != null && pointer.press.wasPressedThisFrame;
        }

        /// <summary>Vị trí màn hình hiện tại của con trỏ. Trả về false nếu không có thiết bị trỏ.</summary>
        public static bool TryGetPosition(out Vector2 screenPos)
        {
            Pointer pointer = Pointer.current;
            if (pointer == null)
            {
                screenPos = default;
                return false;
            }
            screenPos = pointer.position.ReadValue();
            return true;
        }
    }
}
