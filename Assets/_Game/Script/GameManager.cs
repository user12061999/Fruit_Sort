using UnityEngine;
using UnityEngine.UI; // uGUI Text. Đổi sang TMP nếu muốn.

namespace FruitSort
{
    /// <summary>
    /// Quản lý điểm, level và cập nhật UI (fill bucket, số dot còn lại, số dot trên belt).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Refs")]
        public PixelGridManager gridManager;
        public FallingPixelManager fallingManager;

        [Header("Điểm")]
        public int scorePerSorted = 10;   // mỗi dot được hút vào bucket
        public int scorePerBucket = 100;  // mỗi bucket đầy
        public int score = 0;

        [Header("UI (tuỳ chọn)")]
        public Text scoreText;
        public Text dotsLeftText;
        public Text onBeltText;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void Start()
        {
            if (gridManager == null) gridManager = FindFirstObjectByType<PixelGridManager>();
            if (fallingManager == null) fallingManager = FallingPixelManager.Instance;
            RefreshUI();
        }

        void Update()
        {
            RefreshUI(); // số dot biến động liên tục -> cập nhật mỗi frame
        }

        /// <summary>Gọi khi 1 dot được hút vào bucket.</summary>
        public void OnDotSorted(Dot d)
        {
            score += scorePerSorted;
        }

        /// <summary>Gọi khi 1 bucket được lấp đầy.</summary>
        public void OnBucketFilled(Bucket b)
        {
            score += scorePerBucket;
        }

        void RefreshUI()
        {
            if (scoreText != null) scoreText.text = $"Score: {score}";
            if (dotsLeftText != null && gridManager != null) dotsLeftText.text = $"Dots: {gridManager.AliveCount}";
            if (onBeltText != null && fallingManager != null) onBeltText.text = $"On belt: {fallingManager.ActiveCount}";
        }
    }
}
