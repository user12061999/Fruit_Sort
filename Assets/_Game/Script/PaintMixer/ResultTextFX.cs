using UnityEngine;
using TMPro;
using DG.Tweening;

namespace FruitSort
{
    /// <summary>
    /// Text nổi lên và fade out khi kết quả đúng/sai.
    /// Spawn tại vị trí bucket, tự hủy sau animation.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class ResultTextFX : MonoBehaviour
    {
        [Header("Animation")]
        public float floatDistance = 1.8f;
        public float duration = 1.4f;
        public float startScale = 0.6f;
        public float peakScale = 1.0f;

        TextMeshPro _tmp;

        void Awake()
        {
            _tmp = GetComponent<TextMeshPro>();
        }

        /// <summary>
        /// Khởi động hiệu ứng. Gọi ngay sau Instantiate.
        /// </summary>
        public void Play(string message, Color color, bool isSuccess)
        {
            _tmp.text = message;
            _tmp.color = color;

            transform.localScale = Vector3.one * startScale;

            // Nổi lên
            transform.DOMove(transform.position + Vector3.up * floatDistance, duration)
                     .SetEase(Ease.OutCubic);

            // Scale: phình ra rồi nhỏ dần
            transform.DOScale(peakScale, duration * 0.25f)
                     .SetEase(Ease.OutBack)
                     .OnComplete(() =>
                         transform.DOScale(startScale * 0.5f, duration * 0.75f)
                                  .SetEase(Ease.InCubic));

            // Fade out ở nửa sau animation
            _tmp.DOFade(0f, duration * 0.5f)
                .SetDelay(duration * 0.5f)
                .SetEase(Ease.InQuad)
                .OnComplete(() => Destroy(gameObject));

            // Rung nhẹ khi fail
            if (!isSuccess)
            {
                transform.DOShakePosition(0.3f, 0.08f, 15, 90f, false, true)
                         .SetDelay(0f);
            }
        }

        /// <summary>Spawn và tự play tại worldPosition.</summary>
        public static ResultTextFX Spawn(ResultTextFX prefab, Vector3 worldPos,
                                         string message, Color color, bool isSuccess)
        {
            if (prefab == null) return null;
            var fx = Instantiate(prefab, worldPos, Quaternion.identity);
            fx.Play(message, color, isSuccess);
            return fx;
        }
    }
}
