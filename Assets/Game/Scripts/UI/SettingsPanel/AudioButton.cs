using UnityEngine;
using UnityEngine.UI;
using HAVIGAME.UI;

[RequireComponent(typeof(Toggle))]
public abstract class AudioButton : MonoBehaviour {
    [SerializeField] private UIStateTransition transition;

    private Toggle toggle;

    protected abstract bool Volume { get; set; }

    private void Awake() {
        toggle = GetComponent<Toggle>();
        transition.Initialize();
    }

    private void Start() {
        toggle.isOn = Volume;
        SyncUIState();
        toggle.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnDestroy() {
        toggle.onValueChanged.RemoveListener(OnValueChanged);
    }

    private void OnValueChanged(bool value) {
        Volume = value;
        SyncUIState();
    }

    private void SyncUIState() {
        if (transition != null) {
            if (toggle.isOn) {
                transition.PlayShowAnimation(null);
            } else {
                transition.PlayHideAnimation(null);
            }
        }
    }
}
