using UnityEngine;

namespace FruitSort
{
    /// <summary>
    /// Gắn vào Dot prefab để liên kết FruitData với hệ CMY.
    /// Tự tra cứu FruitData từ FruitDatabase theo colorId của Dot khi Start().
    /// BlenderSlot đọc CMYVector khi xay quả thành PaintStream.
    /// </summary>
    [DisallowMultipleComponent]
    public class FruitComponent : MonoBehaviour
    {
        [Tooltip("FruitData chứa thông tin loại quả và CMY. Tự điền khi có FruitDatabase.")]
        public FruitData fruitData;

        [Tooltip("Database để tra cứu FruitData theo colorId của Dot.")]
        public FruitDatabase fruitDatabase;

        /// <summary>Vector CMY (0-1) đại diện cho lượng màu của quả này.</summary>
        public Vector3 CMYVector
        {
            get
            {
                if (fruitData == null) return Vector3.one;
                if (fruitData.colorType == FruitColorType.Mixed)
                    return Vector3.one;
                return fruitData.cmyVector;
            }
        }

        public Color DisplayColor => PaintColorUtility.CMYToRGB(CMYVector);

        /// <summary>
        /// Khởi tạo từ Dot: tra cứu FruitData theo dot.colorId trong FruitDatabase.
        /// Gọi sau khi Dot.Init() đã set colorId.
        /// </summary>
        public void InitFromDot(Dot dot, FruitDatabase db)
        {
            if (dot == null || db == null) return;
            fruitDatabase = db;
            fruitData = db.GetById(dot.colorId);
            ApplyVisual();
        }

        void Start()
        {
            // Nếu chưa có fruitData, tự tra cứu từ Dot + Database
            if (fruitData == null)
            {
                var dot = GetComponent<Dot>();
                if (dot != null && fruitDatabase != null)
                {
                    fruitData = fruitDatabase.GetById(dot.colorId);
                    ApplyVisual();
                }
            }
            else
            {
                ApplyVisual();
            }
        }

        void ApplyVisual()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && fruitData != null)
                sr.color = fruitData.color;
        }

        void OnValidate()
        {
            ApplyVisual();
        }
    }
}
