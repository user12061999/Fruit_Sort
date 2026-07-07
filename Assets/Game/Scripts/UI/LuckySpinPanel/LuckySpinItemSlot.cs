using HAVIGAME;
using HAVIGAME.UI;
using UnityEngine;

public class LuckySpinItemSlot : LuckySpinSlot {
    [SerializeField] private ItemStack itemReward;
    [SerializeField, ButtonField("Update", "Show")] private ItemView itemView;

    public override void Show() {
        base.Show();

        itemView.SetModel(itemReward);
        itemView.Show();
    }

    public override void Collect() {
        UIManager.Instance.Push<GetRewardPanel>().SetRewards(1, itemReward);
    }
}
