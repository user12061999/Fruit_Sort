using UnityEngine;

namespace FruitSort
{
    /// <summary>
    /// Các hàm tĩnh xử lý màu sắc subtractive CMY cho hệ thống Họa Sĩ Pha Chế.
    /// </summary>
    public static class PaintColorUtility
    {
        public const float MUD_THRESHOLD = 0.3f;

        /// <summary>Chuyển CMY (0-1) sang RGB subtractive.</summary>
        public static Color CMYToRGB(Vector3 cmy)
        {
            float r = 1f - Mathf.Clamp01(cmy.x);
            float g = 1f - Mathf.Clamp01(cmy.y);
            float b = 1f - Mathf.Clamp01(cmy.z);
            return new Color(r, g, b);
        }

        /// <summary>Trộn hai bộ CMY theo trọng số volume.</summary>
        public static Vector3 MixCMY(Vector3 a, float volA, Vector3 b, float volB)
        {
            float total = volA + volB;
            if (total < 0.0001f) return Vector3.zero;
            return new Vector3(
                (a.x * volA + b.x * volB) / total,
                (a.y * volA + b.y * volB) / total,
                (a.z * volA + b.z * volB) / total
            );
        }

        /// <summary>Kiểm tra hỗn hợp CMY có tạo bùn không (cả 3 kênh đều lớn).</summary>
        public static bool IsMud(Vector3 cmy)
        {
            return cmy.x > MUD_THRESHOLD && cmy.y > MUD_THRESHOLD && cmy.z > MUD_THRESHOLD;
        }

        public static Vector3 GetMudCMY() => new Vector3(0.5f, 0.5f, 0.5f);

        /// <summary>Khoảng cách Euclidean giữa hai màu RGB (0 = giống hệt, 1.73 = xa nhất).</summary>
        public static float ColorDistance(Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }

        /// <summary>Tên màu hiển thị từ vector CMY.</summary>
        public static string GetColorName(Vector3 cmy)
        {
            if (IsMud(cmy)) return "Bùn";
            float c = cmy.x, m = cmy.y, y = cmy.z;
            float total = c + m + y;
            if (total < 0.1f) return "Trắng";
            if (c > 0.7f && m > 0.7f && y > 0.7f) return "Đen";
            if (c > 0.6f && m < 0.3f && y < 0.3f) return "Lục lam";
            if (m > 0.6f && c < 0.3f && y < 0.3f) return "Hồng cánh sen";
            if (y > 0.6f && c < 0.3f && m < 0.3f) return "Vàng";
            if (c > 0.5f && m > 0.5f && y < 0.3f) return "Xanh lam";
            if (c > 0.5f && y > 0.5f && m < 0.3f) return "Xanh lá";
            if (m > 0.5f && y > 0.5f && c < 0.3f) return "Đỏ";
            return "Hỗn hợp";
        }
    }
}
