using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace FruitSort
{
    /// <summary>
    /// Đơn hàng: hiển thị một màu mục tiêu. Người chơi kéo-thả bình lên đây để giao.
    /// Nếu màu bình khớp màu mục tiêu (trong dung sai) => cộng điểm và sinh đơn mới;
    /// sai màu => báo lỗi, bình giữ nguyên nước và quay về.
    /// Cần Collider2D (trigger) để BottleDragController nhận biết khi thả lên.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class JuiceOrder : MonoBehaviour
    {
        [Header("Đơn hàng")]
        [Tooltip("Màu mục tiêu cần pha. Có thể để JuiceGameManager random.")]
        public Color targetColor = Color.red;
        [Tooltip("Dung sai màu (0 = phải chính xác).")]
        [Range(0f, 1f)] public float tolerance = 0.18f;
        [Tooltip("Điểm khi giao đúng.")]
        public int scoreOnCorrect = 100;
        [Tooltip("Thời gian chờ trước khi sinh đơn mới.")]
        public float respawnDelay = 0.5f;

        [Header("Visual")]
        [Tooltip("SpriteRenderer hiển thị màu mục tiêu (world-space).")]
        public SpriteRenderer preview;
        [Tooltip("Image hiển thị màu mục tiêu (UI). Tuỳ chọn.")]
        public Image previewImage;

        Collider2D _col;

        void Awake()
        {
            _col = GetComponent<Collider2D>();
            if (_col != null) _col.isTrigger = true;
        }

        void Start()
        {
            RefreshVisual();
        }

        public void SetNewOrder(Color color)
        {
            targetColor = color;
            RefreshVisual();
            transform.DOKill();
            transform.DOPunchScale(Vector3.one * 0.3f, 0.4f, 6, 0.5f);
        }

        void RefreshVisual()
        {
            Color shown = JuiceColorUtility.Vivid(targetColor);
            if (preview != null) preview.color = shown;
            if (previewImage != null) previewImage.color = shown;
        }

        /// <summary>
        /// Thử giao bình. Trả về true nếu đã xử lý (đúng màu) — khi đó bình được làm rỗng.
        /// Sai màu trả về false và bình giữ nguyên.
        /// </summary>
        public bool TryDeliver(JuiceBottle bottle)
        {
            if (bottle == null || bottle.IsEmpty) return false;

            // So khớp trên màu ĐÃ tăng tươi (đúng những gì người chơi nhìn thấy).
            float dist = JuiceColorUtility.Distance(
                JuiceColorUtility.Vivid(bottle.juiceColor),
                JuiceColorUtility.Vivid(targetColor));
            if (dist <= tolerance)
            {
                // Đúng màu.
                bottle.Empty();
                transform.DOKill();
                transform.DOPunchScale(Vector3.one * 0.5f, 0.5f, 8, 0.8f);

                if (JuiceGameManager.Instance != null)
                {
                    JuiceGameManager.Instance.AddScore(scoreOnCorrect);
                    DOVirtual.DelayedCall(respawnDelay, () => JuiceGameManager.Instance.RequestNewOrder(this));
                }
                return true;
            }

            // Sai màu.
            transform.DOKill();
            transform.DOShakePosition(0.35f, 0.15f, 18, 90f, false, true);
            return false;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0.5f);
            var col = GetComponent<Collider2D>();
            if (col != null) Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
