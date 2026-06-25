using UnityEngine;
using TMPro;

namespace FruitSort
{
    /// <summary>
    /// Hiển thị tên màu mục tiêu và ô màu mẫu phía trên OrderBucket.
    /// Tự cập nhật mỗi frame khi targetCMY thay đổi.
    /// </summary>
    public class OrderBucketLabel : MonoBehaviour
    {
        [Header("Refs (tự tìm nếu để trống)")]
        public OrderBucket bucket;
        public TextMeshPro colorNameTMP;
        public SpriteRenderer colorSwatch;

        [Header("Visual")]
        public Color textColor = Color.white;
        public Color bgColor = new Color(0f, 0f, 0f, 0.6f);

        Vector3 _lastCMY = Vector3.one * -1f; // sentinel

        void Awake()
        {
            if (bucket == null)
                bucket = GetComponentInParent<OrderBucket>();

            if (colorNameTMP == null)
                colorNameTMP = GetComponentInChildren<TextMeshPro>();

            if (colorSwatch == null)
            {
                var swatchGO = transform.Find("Swatch");
                if (swatchGO != null)
                    colorSwatch = swatchGO.GetComponent<SpriteRenderer>();
            }
        }

        void Update()
        {
            if (bucket == null) return;
            if (bucket.targetCMY == _lastCMY) return;

            _lastCMY = bucket.targetCMY;
            Refresh();
        }

        public void Refresh()
        {
            if (bucket == null) return;

            Color displayColor = PaintColorUtility.CMYToRGB(bucket.targetCMY);
            string colorName   = PaintColorUtility.GetColorName(bucket.targetCMY);

            if (colorNameTMP != null)
            {
                colorNameTMP.text  = colorName;
                colorNameTMP.color = textColor;
            }

            if (colorSwatch != null)
            {
                colorSwatch.color = displayColor;
            }

            // Tô viền bucket theo màu mục tiêu
            if (bucket.body != null)
                bucket.body.color = Color.Lerp(Color.white, displayColor, 0.5f);
        }

        void OnEnable() => Refresh();
    }
}
