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
        public FallingPixelManager fallingManager;

        [Header("Điểm")]
        public int scorePerSorted = 10;   // mỗi dot được hút vào bucket
        public int scorePerBucket = 100;  // mỗi bucket đầy
        public int score = 0;

        [Header("UI (tuỳ chọn)")]
        public Text scoreText;
        public Text onBeltText;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void Start()
        {
            if (fallingManager == null) fallingManager = FallingPixelManager.Instance;
            RefreshUI();
        }

        // Cache để chỉ ghi .text khi giá trị đổi (tránh GC chuỗi + Canvas rebuild mỗi frame).
        int _lastScore = int.MinValue, _lastOnBelt = int.MinValue;

        void Update()
        {
            RefreshUI(); // chỉ thực sự ghi .text khi có thay đổi
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
            if (scoreText != null && score != _lastScore)
            {
                scoreText.text = $"Score: {score}";
                _lastScore = score;
            }
            if (onBeltText != null && fallingManager != null)
            {
                int v = fallingManager.ActiveCount;
                if (v != _lastOnBelt) { onBeltText.text = $"On belt: {v}"; _lastOnBelt = v; }
            }
        }
    }
}
