using HAVIGAME.UI;
using System;
using UnityEngine;
using UnityEngine.UI;

public class AvatarView : View<Avatar> {
    [SerializeField] private Image imgIcon;
    [SerializeField] private Button btnSelect;

    public Action<AvatarView> onSelected;

    private void Start() {
        if (btnSelect) btnSelect.onClick.AddListener(Select);
    }

    public override void Show() {
        if (imgIcon) imgIcon.sprite = Model.Icon;
    }

    private void Select() {
        onSelected?.Invoke(this);
    }
}
