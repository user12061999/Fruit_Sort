using UnityEngine;
using TMPro;

namespace HAVIGAME.Services.Advertisings {
    public class NativeAdTMProTextView : NativeAdStringElementView {
        [SerializeField] private TMP_Text txtContent;

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
