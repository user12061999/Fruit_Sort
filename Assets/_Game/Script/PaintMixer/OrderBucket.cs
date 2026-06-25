using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace FruitSort
{
    /// <summary>
    /// Bucket đơn hàng: hiển thị màu mục tiêu và HÚT dòng sơn (PaintStream) khớp màu
    /// từ băng chuyền lên. Dòng sơn sai màu được bỏ qua để tiếp tục chạy vòng,
    /// người chơi dùng các module (Mixer/Syringe/Filter) chỉnh lại rồi thử tiếp.
    /// Đặt phía trên đường băng chuyền; attractRadius cần đủ lớn để chạm tới dòng sơn.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class OrderBucket : MonoBehaviour
    {
        [Header("Đơn hàng")]
        [Tooltip("CMY mục tiêu (điền tay hoặc để PaintGameManager random).")]
        public Vector3 targetCMY = new Vector3(1f, 0f, 0f);
        [Tooltip("Dung sai màu (0 = phải chính xác, 0.3 = chấp nhận gần đúng).")]
        [Range(0f, 1f)] public float colorTolerance = 0.25f;
        [Tooltip("Điểm cộng khi hút đúng màu.")]
        public int scoreOnCorrect = 100;
        [Tooltip("Thời gian (giây) trước khi sinh đơn hàng mới sau khi hoàn thành.")]
        public float respawnDelay = 3f;

        [Header("Hút dòng sơn")]
        [Tooltip("Bán kính hút dòng sơn từ băng chuyền lên bucket.")]
        public float attractRadius = 1.8f;
        [Tooltip("Thời gian kéo dòng sơn vào miệng bucket.")]
        public float pullDuration = 0.35f;

        [Header("UI")]
        [Tooltip("Image hiển thị màu mục tiêu (gán từ Inspector).")]
        public Image colorPreviewImage;
        [Tooltip("Text hiển thị tên màu mục tiêu.")]
        public Text colorNameText;
        [Tooltip("Text hiển thị thông báo kết quả.")]
        public Text resultText;

        [Header("Visual Feedback")]
        [Tooltip("SpriteRenderer thân bucket để animate.")]
        public SpriteRenderer body;

        [Header("Result FX")]
        [Tooltip("Prefab ResultTextFX hiển thị kết quả đúng/sai.")]
        public ResultTextFX resultFXPrefab;

        Collider2D _col;
        bool _completed = false;

        public bool IsCompleted => _completed;

        static readonly Color SuccessColor = new Color(0.2f, 1f, 0.3f);

        void Awake()
        {
            _col = GetComponent<Collider2D>();
            if (_col != null) _col.isTrigger = true;
            if (body == null) body = GetComponent<SpriteRenderer>();
        }

        void Start()
        {
            RefreshOrderUI();
        }

        /// <summary>Đặt màu đơn hàng mới và reset trạng thái.</summary>
        public void SetNewOrder(Vector3 cmy)
        {
            targetCMY = cmy;
            _completed = false;
            RefreshOrderUI();

            // Pop-in animation
            transform.DOKill();
            transform.DOPunchScale(Vector3.one * 0.3f, 0.4f, 6, 0.5f);
        }

        /// <summary>Cập nhật UI màu mục tiêu.</summary>
        void RefreshOrderUI()
        {
            Color target = PaintColorUtility.CMYToRGB(targetCMY);
            if (colorPreviewImage != null) colorPreviewImage.color = target;
            if (colorNameText != null) colorNameText.text = PaintColorUtility.GetColorName(targetCMY);
            if (resultText != null) resultText.text = "";
            if (body != null)
            {
                // Viền bucket tô màu mục tiêu
                body.color = Color.Lerp(Color.white, target, 0.4f);
            }
        }

        void Update()
        {
            if (_completed) return;
            TryCatchStream();
        }

        /// <summary>Quét các dòng sơn trong bán kính hút; hút dòng đầu tiên khớp màu.</summary>
        void TryCatchStream()
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, attractRadius);
            foreach (var h in hits)
            {
                var stream = h.GetComponent<PaintStream>();
                if (stream == null || stream.IsMerging || stream.IsFrozen) continue;

                Color streamColor = stream.GetDisplayColor();
                Color targetColor = PaintColorUtility.CMYToRGB(targetCMY);
                float dist = PaintColorUtility.ColorDistance(streamColor, targetColor);
                bool isMud = PaintColorUtility.IsMud(stream.cmy);

                if (!isMud && dist <= colorTolerance)
                {
                    CatchStream(stream, dist);
                    return;
                }
                // Sai màu / bùn -> bỏ qua, để dòng sơn tiếp tục chạy vòng.
            }
        }

        /// <summary>Khóa dòng sơn khỏi băng chuyền, hút vào miệng bucket và chấm điểm.</summary>
        void CatchStream(PaintStream stream, float dist)
        {
            _completed = true;

            // Tách khỏi băng chuyền rồi hút vào miệng bucket
            stream.IsFrozen = true;
            stream.transform.DOKill();
            stream.transform.DOMove(transform.position, pullDuration).SetEase(Ease.InBack);
            stream.transform.DOScale(Vector3.zero, pullDuration)
                  .OnComplete(() => { if (stream != null) Destroy(stream.gameObject); });

            // Hiệu ứng ăn mừng
            transform.DOKill();
            transform.DOPunchScale(Vector3.one * 0.5f, 0.5f, 8, 0.8f);
            if (body != null) body.DOColor(SuccessColor, 0.2f);

            if (resultText != null)
                resultText.text = $"Chính xác!\n±{dist:F2}";

            if (resultFXPrefab != null)
                ResultTextFX.Spawn(resultFXPrefab, transform.position + Vector3.up * 1.5f,
                    $"+{scoreOnCorrect}\nChính xác!", SuccessColor, true);

            if (PaintGameManager.Instance != null)
                PaintGameManager.Instance.AddScore(scoreOnCorrect);

            Debug.Log($"[OrderBucket] Hút đúng màu! dist={dist:F3}, +{scoreOnCorrect}đ");

            // Sinh đơn hàng mới
            Invoke(nameof(RequestNewOrder), respawnDelay);
        }

        void RequestNewOrder()
        {
            if (PaintGameManager.Instance != null)
                PaintGameManager.Instance.RequestNewOrder(this);
        }

        void OnDrawGizmosSelected()
        {
            Color c = PaintColorUtility.CMYToRGB(targetCMY);
            Gizmos.color = new Color(c.r, c.g, c.b, 0.4f);
            Gizmos.DrawWireSphere(transform.position, attractRadius);
        }
    }
}
