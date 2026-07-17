using UnityEngine.EventSystems;

namespace FruitSort
{
    /// <summary>
    /// Chặn input gameplay khi con trỏ đang nằm trên UI (uGUI).
    /// Các script tự đọc input qua <see cref="PointerInput"/> (Bucket, ModelDotSpawner,
    /// ModelDotSpawnerColumn) phải hỏi guard này trước khi xử lý click — Input System
    /// không tự chặn click xuyên qua Canvas.
    /// </summary>
    public static class UIPointerGuard
    {
        /// <summary>Con trỏ (chuột / touch) hiện đang trên một phần tử UI raycastable?</summary>
        public static bool IsPointerOverUI()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            if (PointerInput.TryGetActiveTouchFingerId(out int fingerId))
                return eventSystem.IsPointerOverGameObject(fingerId);

            return eventSystem.IsPointerOverGameObject();
        }
    }
}
