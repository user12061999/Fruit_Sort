using UnityEngine;

namespace FruitSort
{
    /// <summary>
    /// Gom input chuột + cảm ứng qua Legacy Input Manager của project.
    /// Gameplay luôn đọc qua lớp này để Android dùng được Input.touch.
    /// </summary>
    public static class PointerInput
    {
        /// <summary>Con trỏ (chuột trái / touch chính) vừa NHẤN XUỐNG trong frame này?</summary>
        public static bool PressedThisFrame()
        {
            if (TryGetPrimaryTouch(out Touch touch))
                return touch.phase == TouchPhase.Began;

            return Input.GetMouseButtonDown(0);
        }

        /// <summary>Vị trí màn hình hiện tại của con trỏ. Trả về false nếu không có thiết bị trỏ.</summary>
        public static bool TryGetPosition(out Vector2 screenPos)
        {
            if (TryGetPrimaryTouch(out Touch touch))
            {
                screenPos = touch.position;
                return true;
            }

            if (Input.mousePresent)
            {
                screenPos = Input.mousePosition;
                return true;
            }

            screenPos = default;
            return false;
        }

        /// <summary>Finger ID của touch đang hoạt động để StandaloneInputModule chặn click UI.</summary>
        public static bool TryGetActiveTouchFingerId(out int fingerId)
        {
            if (TryGetPrimaryTouch(out Touch touch))
            {
                fingerId = touch.fingerId;
                return true;
            }

            fingerId = -1;
            return false;
        }

        static bool TryGetPrimaryTouch(out Touch touch)
        {
            if (Input.touchCount > 0)
            {
                touch = Input.GetTouch(0);
                return true;
            }

            touch = default;
            return false;
        }
    }
}
