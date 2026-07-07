using UnityEngine;
using UnityEngine.UI;

namespace HAVIGAME.UI {
    [RequireComponent(typeof(Button))]
    public class UITabButton : MonoBehaviour {
        [SerializeField] protected UITransition transition;

        protected UITabManager controller;
        protected Button button;
        protected bool isOn;

        public bool IsOn {
            get {
                return isOn;
            }
            set {
                if (isOn != value) {
                    isOn = value;

                    UpdateUI();
                }
            }
        }

        protected virtual void Awake() {
            button = GetComponent<Button>();
        }

        protected virtual void Start() {
            button.onClick.AddListener(OnButtonClicked);
        }

        public void Initialize(UITabManager controller, bool isOn) {
            this.controller = controller;
            this.isOn = isOn;

            UpdateUI();
        }

        public void SetIsOnWithoutNotify(bool isOn) {
            if (this.isOn != isOn) {
                this.isOn = isOn;
                UpdateUI();
            }
        }

        public void ManualButtonClick() {
            OnButtonClicked();
        }

        protected virtual void OnButtonClicked() {
            controller.OnTabSelect(this);
        }

        public virtual void UpdateUI() {
            if (transition) {
                if (IsOn) {
                    transition.PlayShowAnimation(null);
                } else {
                    transition.PlayHideAnimation(null);
                }
            }
        }
    }
}
