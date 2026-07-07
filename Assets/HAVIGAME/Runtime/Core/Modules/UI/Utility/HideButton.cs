using UnityEngine;
using UnityEngine.UI;

namespace HAVIGAME.UI {
    [AddComponentMenu("HAVIGAME/UI/Hide Button")]
    public class HideButton : MonoBehaviour {
        private Button button;
        private UIFrame frame;

        private void Awake() {
            button = GetComponent<Button>();
            frame = GetComponentInParent<UIFrame>();
        }

        private void Start() {
            if (button) {
                button.onClick.AddListener(Hide);
            }
        }

        private void OnDestroy() {
            if (button) {
                button.onClick.RemoveListener(Hide);
            }
        }

        private void Hide() {
            if (frame && frame.IsOnTop) {
                UIManager.Instance.Pop();
            }
        }
    }
}
