using UnityEngine;

namespace HAVIGAME.Services.Advertisings {
    public class NativeAdTextView : NativeAdStringElementView {
        [SerializeField] private TextMesh txtContent;

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
