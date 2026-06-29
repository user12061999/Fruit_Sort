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

        [Tooltip("Sprite hình/model lớn, dùng làm hình đại diện hoặc khuôn.")]
        public Sprite sprite;

        [Tooltip("Sprite dành riêng cho Dot trong grid/băng chuyền (thường là ô vuông). " +
                 "Để trống sẽ dùng sprite mặc định của Dot prefab và tint theo color.")]
        public Sprite dotSprite;
    }
}
