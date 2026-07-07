using HAVIGAME;
using HAVIGAME.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GetRewardPanel : UIFrame {
    [Header("[References]")]
    [SerializeField] private ItemView prefab;
    [SerializeField] private Transform container;
    [SerializeField] private Button btnClaim;
    [SerializeField] private Button btnClaimBonus;
    [SerializeField] private TextMeshProUGUI txtBonus;

    private CollectionView<ItemView, ItemStack> itemViews;
    private ItemStack[] rewards;
    private int bonus;

    private void Awake() {
        itemViews = new CollectionView<ItemView, ItemStack>(prefab, container);
    }

    private void Start() {
        btnClaim.onClick.AddListener(Claim);
        btnClaimBonus.onClick.AddListener(ClaimWithBonus);
    }

    public void SetRewards(int bonus, params ItemStack[] itemStacks) {
        this.rewards = itemStacks;
        itemViews.SetModels(itemStacks);
        itemViews.Show();

        btnClaimBonus.gameObject.SetActive(bonus > 1);
        if (bonus > 1) {
            txtBonus.text = Utility.Text.Format("Claim x{0}", bonus);
        }
    }

    public void SetRewards(int bonus, IEnumerable<ItemStack> itemStacks) {
        this.rewards = itemStacks.ToArray();
        itemViews.SetModels(itemStacks);
        itemViews.Show();

        btnClaimBonus.gameObject.SetActive(bonus > 1);
        if (bonus > 1) {
            txtBonus.text = Utility.Text.Format("Claim x{0}", bonus);
        }
    }

    protected override void OnBack() { }

    private void Claim() {
        GameData.Inventory.Add(rewards);
        Hide();
    }

    private void ClaimWithBonus() {
        GameAdvertising.TryShowRewardedAd(() => {
            for (int i = 0; i < rewards.Length; i++) {
                ItemStack reward = rewards[i];
                rewards[i] = new ItemStack(reward.Id, reward.Amount * bonus);
            }

            GameData.Inventory.Add(rewards);
            Hide();
        });
    }
}
