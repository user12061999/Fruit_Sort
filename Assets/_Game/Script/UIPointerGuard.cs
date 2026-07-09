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
            // Parameterless (pointerId -1): với InputSystemUIInputModule nghĩa là
            // "bất kỳ con trỏ nào" -> đúng cho cả chuột lẫn touch.
            return eventSystem != null && eventSystem.IsPointerOverGameObject();
        }
    }
}
