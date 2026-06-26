using UnityEngine;

namespace FruitSort
{
    /// <summary>
    /// Hàm tĩnh xử lý màu nước ép theo mô hình TRỘN THEO THỂ TÍCH (weighted average) —
    /// giống pha nước/sơn thật: màu kết quả = trung bình các kênh RGB có trọng số là thể tích.
    /// Nhờ vậy chỉ từ 3 màu gốc R, G, B có thể pha ra TOÀN BỘ dải màu trong tam giác RGB
    /// (cam, tím, hồng, xanh ngọc, ô-liu, xám…) tùy theo tỉ lệ pha, không bị giới hạn 8 màu.
    /// </summary>
    public static class JuiceColorUtility
    {
        /// <summary>Trộn hai màu theo thể tích: kết quả = (a*volA + b*volB) / (volA+volB).</summary>
        public static Color MixRGB(Color a, float volA, Color b, float volB)
        {
            float total = volA + volB;
            if (total <= 0.0001f) return a;
            float r = (a.r * volA + b.r * volB) / total;
            float g = (a.g * volA + b.g * volB) / total;
            float bl = (a.b * volA + b.b * volB) / total;
            return new Color(r, g, bl, 1f);
        }

        /// <summary>
        /// Tăng độ tươi: chuẩn hóa để kênh sáng nhất = 1 (giữ nguyên sắc màu & độ bão hòa, kéo độ
        /// sáng lên tối đa). Nhờ vậy màu pha bị "đục" do trộn theo thể tích (vd ô-liu 0.5,0.5,0)
        /// hiển thị rực rỡ (vàng 1,1,0), cam, tím, hồng… đều tươi. Dùng cho HIỂN THỊ và SO KHỚP,
        /// không đổi màu lưu trong bình nên phép trộn vẫn chuẩn xác.
        /// amount: 0 = giữ nguyên, 1 = tươi tối đa.
        /// </summary>
        public static Color Vivid(Color c, float amount = 1f)
        {
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            if (max <= 0.0001f) return c;
            float k = 1f / max;
            return new Color(
                Mathf.Lerp(c.r, c.r * k, amount),
                Mathf.Lerp(c.g, c.g * k, amount),
                Mathf.Lerp(c.b, c.b * k, amount),
                c.a);
        }

        /// <summary>Khoảng cách Euclidean giữa hai màu RGB (0 = giống hệt, ~1.73 = xa nhất).</summary>
        public static float Distance(Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }

        /// <summary>Hai màu có đủ gần để coi là khớp (theo dung sai)?</summary>
        public static bool Match(Color a, Color b, float tolerance)
        {
            return Distance(a, b) <= tolerance;
        }
    }
}
