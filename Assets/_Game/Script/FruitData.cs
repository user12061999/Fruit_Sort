using UnityEngine;

namespace FruitSort
{
    public enum FruitColorType
    {
        Cyan,
        Magenta,
        Yellow,
        Black,
        White,
        Mixed
    }

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

        [Header("Hệ màu CMY (Họa Sĩ Pha Chế)")]
        [Tooltip("Loại màu CMY: Cyan/Magenta/Yellow là gốc, Black/White đặc biệt, Mixed là màu nhiễu.")]
        public FruitColorType colorType = FruitColorType.Mixed;

        [Tooltip("Vector CMY (x=Cyan, y=Magenta, z=Yellow) từ 0-1. Mixed mặc định là (1,1,1) tạo bùn.")]
        public Vector3 cmyVector = new Vector3(1f, 1f, 1f);

#if UNITY_EDITOR
        void OnValidate()
        {
            // Tự điền cmyVector mặc định theo colorType khi chỉnh trong Inspector.
            switch (colorType)
            {
                case FruitColorType.Cyan:    cmyVector = new Vector3(1f, 0f, 0f); break;
                case FruitColorType.Magenta: cmyVector = new Vector3(0f, 1f, 0f); break;
                case FruitColorType.Yellow:  cmyVector = new Vector3(0f, 0f, 1f); break;
                case FruitColorType.Black:   cmyVector = new Vector3(1f, 1f, 1f); break;
                case FruitColorType.White:   cmyVector = new Vector3(0f, 0f, 0f); break;
                case FruitColorType.Mixed:   cmyVector = new Vector3(1f, 1f, 1f); break;
            }
        }
#endif
    }
}
