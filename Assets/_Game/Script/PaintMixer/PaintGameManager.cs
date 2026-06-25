using UnityEngine;
using UnityEngine.UI;

namespace FruitSort
{
    /// <summary>
    /// Quản lý điểm số và đơn hàng cho chế độ Họa Sĩ Pha Chế.
    /// Tự động tạo đơn hàng ngẫu nhiên cho các OrderBucket.
    /// </summary>
    public class PaintGameManager : MonoBehaviour
    {
        public static PaintGameManager Instance { get; private set; }

        [Header("Refs")]
        [Tooltip("Danh sách các OrderBucket trong scene.")]
        public OrderBucket[] buckets;

        [Header("Đơn hàng")]
        [Tooltip("Danh sách màu CMY khả dụng để sinh đơn hàng.")]
        public Vector3[] orderPresets = new Vector3[]
        {
            new Vector3(1f, 0f, 0f),   // Cyan
            new Vector3(0f, 1f, 0f),   // Magenta
            new Vector3(0f, 0f, 1f),   // Vàng
            new Vector3(1f, 1f, 0f),   // Xanh lam (C+M)
            new Vector3(1f, 0f, 1f),   // Xanh lá (C+Y)
            new Vector3(0f, 1f, 1f),   // Đỏ (M+Y)
        };

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
            // Tự tìm buckets nếu chưa gán
            if (buckets == null || buckets.Length == 0)
                buckets = FindObjectsByType<OrderBucket>(FindObjectsInactive.Exclude);

            // Phát đơn hàng ban đầu
            foreach (var b in buckets)
                if (b != null && !b.IsCompleted)
                    AssignRandomOrder(b);

            RefreshUI();
        }

        void Update()
        {
            if (score != _lastScore)
            {
                _lastScore = score;
                RefreshUI();
            }
        }

        /// <summary>Cộng/trừ điểm.</summary>
        public void AddScore(int delta)
        {
            score = Mathf.Max(0, score + delta);
            if (score > highScore) highScore = score;
        }

        /// <summary>Gọi từ OrderBucket khi cần đơn hàng mới.</summary>
        public void RequestNewOrder(OrderBucket bucket)
        {
            if (bucket == null) return;
            AssignRandomOrder(bucket);
        }

        void AssignRandomOrder(OrderBucket bucket)
        {
            if (orderPresets == null || orderPresets.Length == 0) return;
            Vector3 chosen = orderPresets[Random.Range(0, orderPresets.Length)];
            bucket.SetNewOrder(chosen);
        }

        void RefreshUI()
        {
            if (scoreText != null) scoreText.text = $"Điểm: {score}";
            if (highScoreText != null) highScoreText.text = $"Cao nhất: {highScore}";
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Cộng 100 điểm")]
        void DebugAddScore() => AddScore(100);

        [ContextMenu("Debug: Reset điểm")]
        void DebugResetScore() { score = 0; highScore = 0; }
#endif
    }
}
