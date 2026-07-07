using UnityEngine;
using UnityEngine.UI;
using System;
using HAVIGAME.UI;

public class DailyRewardItem : MonoBehaviour {
    [SerializeField] private int day;
    [SerializeField] private ItemStack[] rewards;
    [SerializeField] private UIStateTransition collectTransition;
    [SerializeField] private UIStateTransition highlightTransition;
    [SerializeField] private Button btnClaim;

    public Action<DailyRewardItem> onClaim;
    public ItemStack[] Rewards => rewards;

    public int Day => day;

    private void Start() {
        btnClaim.onClick.AddListener(OnClaim);
    }

    public void Show() {
        int compare = GameData.DailyReward.CompareDay(day);

        if (compare < 0) {
            collectTransition.PlayHideAnimation(null);
            highlightTransition.PlayHideAnimation(null);
            btnClaim.interactable = false;
        } else if (compare == 0) {
            bool canCollect = GameData.DailyReward.CanCollect(day);
            btnClaim.interactable = canCollect;
            collectTransition.PlayShowAnimation(null);
            highlightTransition.PlayShowAnimation(null);
        } else {
            collectTransition.PlayShowAnimation(null);
            highlightTransition.PlayHideAnimation(null);
            btnClaim.interactable = false;
        }
    }

    private void OnClaim() {
        onClaim?.Invoke(this);
    }
}

