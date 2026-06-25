using UnityEngine;

namespace FruitSort
{
    public enum ModuleType
    {
        None,
        Blender,   // Xay quả → PaintStream
        Mixer,     // Gộp nhiều PaintStream
        Syringe,   // Bơm thêm/làm nhạt/đậm
        Filter     // Lọc bỏ kênh màu
    }

    /// <summary>
    /// ScriptableObject định nghĩa một loại module có thể đặt vào ModuleSlot.
    /// Tạo bằng Create > FruitSort > ModuleData.
    /// </summary>
    [CreateAssetMenu(fileName = "ModuleData_New", menuName = "FruitSort/ModuleData")]
    public class ModuleData : ScriptableObject
    {
        [Tooltip("Loại module.")]
        public ModuleType moduleType = ModuleType.Blender;

        [Tooltip("Tên hiển thị trong popup chọn module.")]
        public string moduleName = "Module Mới";

        [Tooltip("Mô tả ngắn.")]
        [TextArea(2, 3)]
        public string description = "";

        [Tooltip("Icon hiển thị trên slot.")]
        public Sprite icon;

        [Tooltip("Màu nền của slot khi module này được đặt.")]
        public Color slotColor = Color.gray;
    }
}
