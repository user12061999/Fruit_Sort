using UnityEngine;
using UnityEngine.UI;

namespace FruitSort
{
    /// <summary>
    /// Quản lý điểm và đơn hàng cho chế độ pha nước ép.
    /// Sinh màu mục tiêu từ tổ hợp các màu nước gốc (đảm bảo người chơi pha được).
    /// </summary>
    public class JuiceGameManager : MonoBehaviour
    {
        public static JuiceGameManager Instance { get; private set; }

        [Header("Đơn hàng")]
        [Tooltip("Các JuiceOrder trong scene. Để trống = tự tìm.")]
        public JuiceOrder[] orders;
        [Tooltip("Các màu nước gốc. Chỉ cần 3 màu R, G, B là pha ra được toàn bộ dải màu.")]
        public Color[] baseColors = new Color[]
        {
            Color.red, Color.green, Color.blue
        };
        [Tooltip("Xác suất đơn hàng là MÀU PHA (trộn 2–3 màu gốc) thay vì 1 màu gốc.")]
        [Range(0f, 1f)] public float mixedOrderChance = 0.7f;
        [Tooltip("Số phần tối đa của mỗi màu gốc khi sinh màu pha (tỉ lệ pha). Càng lớn dải màu càng đa dạng.")]
        [Range(1, 4)] public int maxPartsPerColor = 3;

        [Header("Điểm")]
        public int score = 0;
        public int highScore = 0;

        [Header("UI")]
        public Text scoreText;
        public Text highScoreText;

        int _lastScore = int.MinValue;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void Start()
        {
            if (orders == null || orders.Length == 0)
                orders = FindObjectsByType<JuiceOrder>(FindObjectsInactive.Exclude);

            foreach (var o in orders)
                if (o != null) AssignRandomOrder(o);

            RefreshUI();
        }

        void Update()
        {
            if (score != _lastScore) { _lastScore = score; RefreshUI(); }
        }

        public void AddScore(int delta)
        {
            score = Mathf.Max(0, score + delta);
            if (score > highScore) highScore = score;
        }

        public void RequestNewOrder(JuiceOrder order)
        {
            if (order != null) AssignRandomOrder(order);
        }

        void AssignRandomOrder(JuiceOrder order)
        {
            if (baseColors == null || baseColors.Length == 0) return;

            Color target;
            if (baseColors.Length >= 2 && Random.value < mixedOrderChance)
                target = BuildRandomMix();
            else
                target = baseColors[Random.Range(0, baseColors.Length)];

            order.SetNewOrder(target);
        }

        /// <summary>
        /// Sinh màu mục tiêu bằng cách trộn 2–3 màu gốc theo tỉ lệ PHẦN NGUYÊN (1..maxPartsPerColor),
        /// dùng đúng phép trộn weighted-average của gameplay ⇒ người chơi luôn pha lại được
        /// bằng cách rót các bình theo đúng tỉ lệ đó.
        /// </summary>
        Color BuildRandomMix()
        {
            int n = baseColors.Length;
            int kinds = Mathf.Clamp(Random.Range(2, 4), 2, n); // trộn 2 hoặc 3 màu

            // Chọn 'kinds' chỉ số màu gốc khác nhau (xáo trộn một phần).
            var idx = new int[n];
            for (int i = 0; i < n; i++) idx[i] = i;
            for (int i = 0; i < kinds; i++)
            {
                int j = Random.Range(i, n);
                (idx[i], idx[j]) = (idx[j], idx[i]);
            }

            float r = 0f, g = 0f, b = 0f;
            int total = 0;
            int maxParts = Mathf.Max(1, maxPartsPerColor);
            for (int k = 0; k < kinds; k++)
            {
                int parts = Random.Range(1, maxParts + 1);
                Color c = baseColors[idx[k]];
                r += c.r * parts; g += c.g * parts; b += c.b * parts;
                total += parts;
            }
            return new Color(r / total, g / total, b / total, 1f);
        }

        void RefreshUI()
        {
            if (scoreText != null) scoreText.text = $"Điểm: {score}";
            if (highScoreText != null) highScoreText.text = $"Cao nhất: {highScore}";
        }
    }
}
