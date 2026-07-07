using UnityEngine;
using UnityEngine.UI;

namespace HAVIGAME.Services.Advertisings {
    public class NativeAdUIStarView : NativeAdDoubleElementView {
        [SerializeField] private Image imgContent;

        public override GameObject RegisterGameObject => imgContent.gameObject;

        public override void UpdateElement() {
            float star = 4f;

            if (HasElement && Element.HasData) {
                star = Mathf.Clamp((int)Element.GetData(), 4f, 5f);
            }

            imgContent.type = Image.Type.Tiled;

            float fillAmount = (float)star / 5f;
            RectTransform rectTransform = imgContent.transform as RectTransform;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = new Vector2(fillAmount, 1);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }
    }
}
