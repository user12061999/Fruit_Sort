using UnityEngine;
using UnityEngine.UI;

namespace HAVIGAME.Services.Advertisings {
    public class NativeAdUITextView : NativeAdStringElementView {
        [SerializeField] private Text txtContent;

        public override GameObject RegisterGameObject => txtContent.gameObject;

        public override void UpdateElement() {
            if (HasElement && Element.HasData) {
                txtContent.text = Element.GetData();
            }
            else {
                Hide();
            }
        }
    }
}
