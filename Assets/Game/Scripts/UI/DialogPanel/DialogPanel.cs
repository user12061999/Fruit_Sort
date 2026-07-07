using HAVIGAME.UI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogPanel : UIFrame {
    [Header("[References]")]
    [SerializeField] private TextMeshProUGUI txtTitle;
    [SerializeField] private TextMeshProUGUI txtMessage;
    [SerializeField] private Button btnOk;
    [SerializeField] private Button btnCancel;
    [SerializeField] private TextMeshProUGUI txtOk;
    [SerializeField] private TextMeshProUGUI txtCancel;

    private Action okCallback;
    private Action cancelCallback;

    private void Start() {
        btnOk.onClick.AddListener(Confirm);
        btnCancel.onClick.AddListener(Cancel);
    }

    public void Dialog(string title, string message) {
        txtTitle.text = title;
        txtMessage.text = message;

        txtTitle.enabled = true;
        txtMessage.enabled = true;
        btnOk.gameObject.SetActive(true);
        btnCancel.gameObject.SetActive(false);

        this.okCallback = null;
        this.cancelCallback = null;
    }

    public void Dialog(string title, string message, string ok, Action okCallback = null) {
        txtTitle.text = title;
        txtMessage.text = message;
        txtOk.text = ok;

        txtTitle.enabled = true;
        txtMessage.enabled = true;
        btnOk.gameObject.SetActive(true);
        btnCancel.gameObject.SetActive(false);

        this.okCallback = okCallback;
        this.cancelCallback = null;
    }

    public void Dialog(string title, string message, string ok, string cancel, Action okCallback = null, Action cancelCallback = null) {
        txtTitle.text = title;
        txtMessage.text = message;
        txtOk.text = ok;
        txtCancel.text = cancel;

        txtTitle.enabled = true;
        txtMessage.enabled = true;
        btnOk.gameObject.SetActive(true);
        btnCancel.gameObject.SetActive(true);

        this.okCallback = okCallback;
        this.cancelCallback = cancelCallback;
    }

    private void Confirm() {
        Hide();
        okCallback?.Invoke();
    }

    private void Cancel() {
        Hide();
        cancelCallback?.Invoke();
    }
}
