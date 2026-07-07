using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HAVIGAME;
using HAVIGAME.UI;

public class LuckySpinPanel : UIFrame {
    [SerializeField] private ItemStack luckySpinPrice;
    [Header("[References]")]
    [SerializeField] private LuckySpin luckySpin;
    [SerializeField] private Button btnSpin;
    [SerializeField] private Button btnFreeSpin;
    [SerializeField] private Button btnAdsSpin;
    [SerializeField] private TextMeshProUGUI txtSpinCooldown;
    [SerializeField] private Button btnHide;

    private Timer timer = new Timer();

    private void Start() {
        btnSpin.onClick.AddListener(BuySpin);
        btnFreeSpin.onClick.AddListener(FreeSpin);
        btnAdsSpin.onClick.AddListener(AdsSpin);
        btnHide.onClick.AddListener(HideSpin);
    }

    private void OnDestroy() {
        timer.Stop();
    }

    protected override void OnShow(bool instant = false) {
        base.OnShow(instant);

        UpdateSpin();
    }

    protected override void OnHide(bool instant = false) {
        base.OnHide(instant);

        timer.Stop();
    }

    protected override void OnBack() {
        if (luckySpin.IsRotating) return;

        base.Back();
    }

    private void HideSpin() {
        if (luckySpin.IsRotating) return;

        Hide();
    }

    private void UpdateSpin() {
        luckySpin.Refresh();

        LuckySpinSaveData saveData = GameData.LuckySpin;
        bool hasFreeSpin = saveData.HasFreeSpin;

        btnFreeSpin.gameObject.SetActive(hasFreeSpin);
        btnSpin.gameObject.SetActive(!hasFreeSpin);

        if (!hasFreeSpin) {
            TimeSpan cooldownTime = saveData.GetLeftCooldownTime();
            int totalScecond = (int)cooldownTime.TotalSeconds + 1;

            timer.Countdown(totalScecond, () => {
                TimeSpan leftTime = new TimeSpan(timer.Remaining * TimeSpan.TicksPerSecond);
                if (leftTime.TotalHours >= 1) {
                    txtSpinCooldown.text = Utility.Text.Format("Free in {0}", leftTime.ToString(@"hh\:mm\:ss"));
                } else {
                    txtSpinCooldown.text = Utility.Text.Format("Free in {0}", leftTime.ToString(@"mm\:ss"));
                }
            }, UpdateSpin, true);
        } else {
            txtSpinCooldown.text = string.Empty;
            timer.Stop();
        }

        luckySpin.AutoRotate();
    }

    private void BuySpin() {
        if (luckySpin.IsRotating) return;

        if (GameData.Inventory.IsEnought(luckySpinPrice)) {
            if (luckySpin.Rotate(OnBuySpinCompleted)) {
                GameData.Inventory.Remove(luckySpinPrice);
            }
        } else {
            UIManager.Instance.Push<DialogPanel>().Dialog("Failed!", string.Format("Not enought {0}!", ItemDatabase.Instance.GetDataById(luckySpinPrice.Id).Name));
        }
    }

    private void FreeSpin() {
        if (luckySpin.IsRotating) return;

        luckySpin.Rotate(OnFreeSpinCompleted);
    }

    private void AdsSpin() {
        if (luckySpin.IsRotating) return;

        GameAdvertising.TryShowRewardedAd(() => {
            luckySpin.Rotate(OnAdsSpinCompleted);
        });
    }

    private void OnAdsSpinCompleted(LuckySpinSlot slot) {
        slot.Collect();
        GameData.LuckySpin.OnAdsSpinCompleted();
        UpdateSpin();
    }

    private void OnFreeSpinCompleted(LuckySpinSlot slot) {
        slot.Collect();
        GameData.LuckySpin.OnFreeSpinCompleted();
        UpdateSpin();
    }


    private void OnBuySpinCompleted(LuckySpinSlot slot) {
        slot.Collect();
        UpdateSpin();
    }
}
