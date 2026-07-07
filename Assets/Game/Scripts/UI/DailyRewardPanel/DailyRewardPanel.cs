using HAVIGAME;
using HAVIGAME.UI;
using System;
using TMPro;
using UnityEngine;

public class DailyRewardPanel : UIFrame {
    [Header("[References]")]
    [SerializeField] private DailyRewardItem[] items;
    [SerializeField] private TextMeshProUGUI txtCooldown;

    private Timer timer = new Timer();

    protected override void OnShow(bool instant = false) {
        base.OnShow(instant);

        UpdateDailyRewardInfomation();
    }

    protected override void OnHide(bool instant = false) {
        base.OnHide(instant);

        timer.Stop();
    }

    private void UpdateDailyRewardInfomation() {
        foreach (var item in items) {
            item.Show();
            item.onClaim = OnClaim;
        }

        int currentDay = GameData.DailyReward.DayUnlocked;
        bool canCollect = GameData.DailyReward.CanCollect(currentDay);

        if (canCollect) {
            timer.Stop();
            txtCooldown.text = "Rewards is ready";
        } else {
            TimeSpan cooldownTime = GameData.DailyReward.GetLeftTime();
            int totalScecond = (int)cooldownTime.TotalSeconds + 1;

            timer.Countdown(totalScecond, () => {
                TimeSpan leftTime = new TimeSpan(timer.Remaining * TimeSpan.TicksPerSecond);
                if (leftTime.TotalHours >= 1) {
                    txtCooldown.text = Utility.Text.Format("Next reward is in {0}", leftTime.ToString(@"hh\:mm\:ss"));
                } else {
                    txtCooldown.text = Utility.Text.Format("Next reward is in {0}", leftTime.ToString(@"mm\:ss"));
                }
            }, UpdateDailyRewardInfomation, true);
        }
    }

    private void OnClaim(DailyRewardItem dailyRewardItem) {
        Hide(true);

        GameData.DailyReward.OnRewardCollected(dailyRewardItem.Day);
        ItemStack[] rewards = dailyRewardItem.Rewards;

        GetRewardPanel getRewardPanel = UIManager.Instance.Push<GetRewardPanel>();
        getRewardPanel.SetRewards(2, rewards);
    }
}
