using HAVIGAME.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ProfilePanel : UIFrame {
    [Header("[References]")]
    [SerializeField] private ProfileView profileView;
    [SerializeField] private AvatarView avatarViewPrefab;
    [SerializeField] private Transform avatarViewContainer;

    private CollectionView<AvatarView, Avatar> avatarViews;

    private void Awake() {
        avatarViews = new CollectionView<AvatarView, Avatar>(avatarViewPrefab, avatarViewContainer);
    }

    protected override void OnShow(bool instant = false) {
        base.OnShow(instant);

        profileView.SetModel(GameData.Player.Profile).Show();

        avatarViews.SetModels(AvatarDatabase.Instance.GetAll()).Show();

        foreach (AvatarView avatarView in avatarViews.GetViews()) {
            avatarView.onSelected = OnAvatarSelected;
        }
    }

    private void OnAvatarSelected(AvatarView avatarView) {
        string currentAvatarId = GameData.Player.Profile.Avatar;

        if (!currentAvatarId.Equals(avatarView.Model.Id)) {
            GameData.Player.ChangeAvatar(avatarView.Model.Id);

            avatarView.Show();
            foreach (AvatarView view in avatarViews.GetViews()) {
                if (currentAvatarId.Equals(view.Model.Id)) {
                    view.Show();
                }
            }

            profileView.SetModel(GameData.Player.Profile).Show();
        }
    }
}
