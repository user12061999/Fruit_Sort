using HAVIGAME;
using HAVIGAME.UI;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : UITab {
    [Header("[References]")]
    [SerializeField] private Button btnQuit;
    [SerializeField] private Button btnRestorePurchase;

    private void Start() {
        btnQuit.onClick.AddListener(QuitGame);
        btnRestorePurchase.onClick.AddListener(RestorePurchase);
    }

    private void QuitGame() {
        GameManager.Quit();
    }

    private void RestorePurchase() {
        GameIAP.RestorePurchases(RestorePurchaseCompleted, RestorePurchaseFailed);
    }

    private void RestorePurchaseCompleted(string[] products) {
        string message = string.Format("Total {0} products have been restored", products.Length);
        UIManager.Instance.Push<DialogPanel>().Dialog("Completed!", message);
    }

    private void RestorePurchaseFailed(string error) {
        UIManager.Instance.Push<DialogPanel>().Dialog("Failed!", error);
    }
}
