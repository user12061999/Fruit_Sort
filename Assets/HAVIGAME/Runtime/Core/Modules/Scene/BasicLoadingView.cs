using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HAVIGAME.Scenes {
    public class BasicLoadingView : LoadingView {
        [SerializeField] private Image fadeImage;
        [SerializeField] private GameObject backgroundGraphic;
        [SerializeField] private Slider loadingSlider;
        [SerializeField] private float fadeDuration = 0.25f;

        public override void Initialize() {
            gameObject.SetActive(false);
        }

        public override void OnStart() {
            gameObject.SetActive(true);
            SetFadeState(true);
            SetBackgroundState(false);
            SetAlpha(fadeImage, 0f);
            SetSliderValue(0);
        }

        public override IEnumerator OnStartLoading() {
            SetBackgroundState(true);
            yield break;
        }

        public override void OnLoading(float progress) {
            SetSliderValue(progress);
        }

        public override IEnumerator OnFinishLoading() {
            SetBackgroundState(false);
            yield break;
        }

        public override void OnFinish() {
            gameObject.SetActive(false);
            SetFadeState(false);
        }

        public override IEnumerator FadeIn() {
            return DoFade(fadeImage, 0, fadeDuration);
        }

        public override IEnumerator FadeOut() {
            return DoFade(fadeImage, 1, fadeDuration);
        }

        public IEnumerator DoFade(Graphic target, float endValue, float duration) {
            float startValue = target.color.a;
            float elapsed = 0f;

            while (elapsed <= duration) {
                SetAlpha(target, Mathf.Lerp(startValue, endValue, elapsed / duration));
                elapsed += Time.deltaTime;
                yield return null;
            }

            SetAlpha(target, endValue);
        }

        public void SetBackgroundState(bool active) {
            if (backgroundGraphic) {
                backgroundGraphic.SetActive(active);
            }
        }

        public void SetSliderValue(float value) {
            if (loadingSlider) {
                loadingSlider.value = value;
            }
        }

        public void SetFadeState(bool enabled) {
            if (fadeImage) {
                fadeImage.enabled = enabled;
            }
        }

        public void SetAlpha(Graphic graphic, float alpha) {
            if (graphic) {
                Color color = graphic.color;
                color.a = alpha;
                graphic.color = color;
            }
        }
    }
}
