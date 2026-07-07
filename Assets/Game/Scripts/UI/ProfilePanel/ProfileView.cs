using HAVIGAME.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ProfileView : View<Profile> {
    [SerializeField] private AvatarView avatarView;
    [SerializeField] private TextMeshProUGUI txtName;
    [SerializeField] private TextMeshProUGUI txtId;
    [SerializeField] private TMP_InputField txtNameInputField;
    [SerializeField] private Button btnChangeName;
    [SerializeField] private Button btnSaveName;
    [SerializeField] private Button btnCopyId;

    private void Start() {
        btnChangeName.onClick.AddListener(ChangeName);
        btnSaveName.onClick.AddListener(SaveName);
        btnCopyId.onClick.AddListener(CopyIdToClipboard);
    }

    public override void Show() {
        Avatar avatar = AvatarDatabase.Instance.GetDataById(Model.Avatar);

        avatarView.SetModel(avatar).Show();
        txtName.text = Model.Name;
        txtId.text = Model.Id;

        btnChangeName.gameObject.SetActive(true);
        btnSaveName.gameObject.SetActive(false);
        txtNameInputField.gameObject.SetActive(false);
    }

    private void ChangeName() {
        btnChangeName.gameObject.SetActive(false);
        btnSaveName.gameObject.SetActive(true);
        txtNameInputField.gameObject.SetActive(true);
        txtNameInputField.text = Model.Name;
        EventSystem.current.SetSelectedGameObject(txtNameInputField.gameObject);
    }

    private void SaveName() {
        string name = txtNameInputField.text;

        GameData.Player.ChangeName(name);

        txtName.text = name;

        btnChangeName.gameObject.SetActive(true);
        btnSaveName.gameObject.SetActive(false);
        txtNameInputField.gameObject.SetActive(false);
    }

    private void CopyIdToClipboard() {
        GUIUtility.systemCopyBuffer = Model.Id;
    }
}
