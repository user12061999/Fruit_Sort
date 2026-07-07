using UnityEngine;

namespace HAVIGAME.UI {
    public class UITab : UIFrame {
        [SerializeField] private int index;

        public int Index => index;

        protected override void OnHideCompleted() {
            InvokeOnHidden();
        }
    }
}
