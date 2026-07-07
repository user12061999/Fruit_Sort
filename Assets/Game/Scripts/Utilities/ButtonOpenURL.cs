using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonOpenURL : MonoBehaviour {
    [SerializeField] private string url;

    private Button btn;

    private void Awake() {
        btn = GetComponent<Button>();
    }

    private void Start() {
        if (btn) btn.onClick.AddListener(OpenURL);
    }

    private void OnDestroy() {
        if (btn) btn.onClick.RemoveListener(OpenURL);
    }

    private void OpenURL() {
        Application.OpenURL(url);
    }
}
