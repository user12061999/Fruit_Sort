using UnityEngine;
using UnityEngine.UI;

namespace HAVIGAME.Services.Advertisings {
    public class NativeAdUIRawImageView : NativeAdTextureElementView {
        [SerializeField] private RawImage imgContent;

        public override GameObject RegisterGameObject => imgContent.gameObject;

        public override void UpdateElement() {
            if (HasElement && Element.HasData) {
                Texture2D texture = Element.GetData();

                if (texture != null) {
                    imgContent.texture = texture;
                }
                else {
                    Hide();
                }
            }
            else {
                Hide();
            }
        }
    }
}
