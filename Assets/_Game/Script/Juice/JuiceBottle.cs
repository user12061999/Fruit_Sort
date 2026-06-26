using UnityEngine;
using DG.Tweening;

namespace FruitSort
{
    /// <summary>
    /// Bình nước ép: chứa một màu (RGB) và thể tích. Hiển thị mực nước bằng cách scale Y
    /// của liquid và tint theo màu. Người chơi kéo-thả bình này lên bình khác để pha màu
    /// (xem BottleDragController). Khi không bị kéo, bình luôn nằm tại homeSlot của nó.
    /// </summary>
    public class JuiceBottle : MonoBehaviour
    {
        [Header("Nội dung")]
        [Tooltip("Màu nước trong bình.")]
        public Color juiceColor = Color.white;
        [Tooltip("Thể tích hiện tại (0 = rỗng).")]
        public float volume = 0f;
        [Tooltip("Thể tích tối đa của bình.")]
        public float maxVolume = 3f;

        [Header("Visual")]
        [Tooltip("SpriteRenderer phần nước (sẽ tint màu và scale Y theo mực nước).")]
        public SpriteRenderer liquid;
        [Tooltip("Thân/viền bình (không đổi màu). Tuỳ chọn.")]
        public SpriteRenderer bottleBody;
        [Tooltip("Gốc để scale mực nước. Để trống = dùng transform của liquid.")]
        public Transform liquidRoot;

        [Header("Hiệu ứng")]
        public float pourDuration = 0.45f;

        // ---- runtime ----
        Vector3 _baseLiquidScale = Vector3.one;
        Transform _scaleTarget;

        /// <summary>Ô đặt bình mà bình sẽ quay về khi không bị kéo.</summary>
        public Transform homeSlot;

        /// <summary>Đang trong hiệu ứng rót (không cho cầm/đổ tiếp).</summary>
        [System.NonSerialized] public bool IsBusy;

        public bool IsEmpty => volume <= 0.001f;
        public bool IsFull => volume >= maxVolume - 0.001f;
        public float FillRatio => maxVolume > 0f ? Mathf.Clamp01(volume / maxVolume) : 0f;

        void Awake()
        {
            _scaleTarget = liquidRoot != null ? liquidRoot : (liquid != null ? liquid.transform : null);
            if (_scaleTarget != null) _baseLiquidScale = _scaleTarget.localScale;
        }

        void Start()
        {
            RefreshVisual();
        }

        /// <summary>Đổ đầy bình bằng một màu mới (thay thế nội dung cũ).</summary>
        public void SetJuice(Color color, float vol)
        {
            juiceColor = color;
            volume = Mathf.Clamp(vol, 0f, maxVolume);
            RefreshVisual();
        }

        /// <summary>Làm rỗng bình.</summary>
        public void Empty()
        {
            volume = 0f;
            RefreshVisual();
        }

        /// <summary>
        /// Đổ toàn bộ nước từ <paramref name="source"/> vào bình này (pha màu RGB theo thể tích).
        /// Sau khi đổ, source rỗng. Trả về true nếu có pha (cả hai có nước hợp lệ).
        /// </summary>
        public bool ReceiveFrom(JuiceBottle source)
        {
            if (source == null || source == this || source.IsEmpty) return false;

            if (IsEmpty)
            {
                juiceColor = source.juiceColor;
                volume = Mathf.Min(maxVolume, source.volume);
            }
            else
            {
                Color mixed = JuiceColorUtility.MixRGB(juiceColor, volume, source.juiceColor, source.volume);
                juiceColor = mixed;
                volume = Mathf.Min(maxVolume, volume + source.volume);
            }

            source.Empty();
            RefreshVisual();

            // Hiệu ứng nảy + nhấp nháy báo đã pha.
            transform.DOKill(true);
            transform.DOPunchScale(Vector3.one * 0.18f, 0.3f, 6, 0.6f);
            if (liquid != null)
            {
                liquid.DOKill();
                liquid.DOColor(Color.white, 0.08f).OnComplete(() => liquid.color = JuiceColorUtility.Vivid(juiceColor));
            }
            return true;
        }

        /// <summary>Cập nhật màu và mực nước.</summary>
        public void RefreshVisual()
        {
            if (liquid != null)
            {
                liquid.color = JuiceColorUtility.Vivid(juiceColor);
                liquid.enabled = !IsEmpty;
            }
            if (_scaleTarget != null)
            {
                Vector3 s = _baseLiquidScale;
                s.y = _baseLiquidScale.y * FillRatio;
                _scaleTarget.localScale = s;
            }
        }

        /// <summary>Bay về vị trí ô của nó.</summary>
        public void ReturnHome(float duration = 0.2f)
        {
            if (homeSlot == null) return;
            transform.DOKill();
            transform.DOMove(homeSlot.position, duration).SetEase(Ease.OutQuad);
        }

        void OnValidate()
        {
            if (Application.isPlaying) return;
            _scaleTarget = liquidRoot != null ? liquidRoot : (liquid != null ? liquid.transform : null);
            if (_scaleTarget != null && _baseLiquidScale == Vector3.zero) _baseLiquidScale = _scaleTarget.localScale;
        }
    }
}
