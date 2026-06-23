using UnityEngine;

namespace FruitSort
{
    /// <summary>
    /// Data 1 loại quả. Tạo bằng Create > FruitSort > FruitData.
    /// colorId phải khớp với Bucket.colorId và colorId của Dot.
    /// </summary>
    [CreateAssetMenu(fileName = "FruitData_New", menuName = "FruitSort/FruitData")]
    public class FruitData : ScriptableObject
    {
        [Tooltip("ID màu, phải khớp với Bucket.colorId và colorId của Dot.")]
        public int colorId = 0;

        [Tooltip("Tên hiển thị (VD: 'Táo', 'Cam').")]
        public string fruitName = "Quả Mới";

        [Tooltip("Màu đại diện — dùng để tint dot và bucket.")]
        public Color color = Color.white;

        [Tooltip("Sprite đại diện cho loại quả này.")]
        public Sprite sprite;
    }
}
