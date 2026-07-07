using HAVIGAME;
using System;
using TMPro;
using UnityEngine;

public class HeartRegenListener : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI txtCooldownTime;
    [SerializeField] private GameObject infinityIcon;

    private Timer timer = new Timer();

    private void Start() {
        UpdateUI();

        EventDispatcher.AddListener<GameEvent.PlayerInventoryChanged>(OnPlayerInventoryChanged);
        EventDispatcher.AddListener<GameEvent.HeartRegenChanged>(OnHeartRegenChanged);
        EventDispatcher.AddListener<GameEvent.InfinityHeartChanged>(OnInfinityHeartChanged);
    }

    private void OnDestroy() {
        EventDispatcher.RemoveListener<GameEvent.PlayerInventoryChanged>(OnPlayerInventoryChanged);
        EventDispatcher.RemoveListener<GameEvent.HeartRegenChanged>(OnHeartRegenChanged);
        EventDispatcher.RemoveListener<GameEvent.InfinityHeartChanged>(OnInfinityHeartChanged);
    }

    private void OnPlayerInventoryChanged(GameEvent.PlayerInventoryChanged e) {
        if (e.ItemStackChange.Id == ItemID.Heart) {
            int maxHeartAmount = ConfigDatabase.Instance.MaxHeart;
            int heartAmount = GameData.Inventory.GetCount(ItemID.Heart);
            bool isRegen = heartAmount < maxHeartAmount;

            if (!GameData.Inventory.IsInfinityHeart) {
                if (isRegen) {

                } else {
                    txtCooldownTime.text = "Full";
                }
            }
        }
    }

    private void OnHeartRegenChanged(GameEvent.HeartRegenChanged e) {
        if (!GameData.Inventory.IsInfinityHeart) {
            TimeSpan timeSpan = TimeSpan.FromSeconds(e.RemainingSeconds);

            if (timeSpan.TotalHours > 1) {
                txtCooldownTime.text = timeSpan.ToString(@"hh\:mm\:ss");
            } else {
                txtCooldownTime.text = timeSpan.ToString(@"mm\:ss");
            }
        }
    }

    private void OnInfinityHeartChanged(GameEvent.InfinityHeartChanged e) {
        infinityIcon.SetActive(e.IsInfinity);

        if (e.IsInfinity) {
            TimeSpan cooldownTime = (e.InfinityTime - DateTime.Now);
            int totalScecond = (int)cooldownTime.TotalSeconds + 1;

            timer.Countdown(totalScecond, () => {
                TimeSpan leftTime = new TimeSpan(timer.Remaining * TimeSpan.TicksPerSecond);
                if (leftTime.TotalHours >= 1) {
                    txtCooldownTime.text = leftTime.ToString(@"hh\:mm\:ss");
                } else {
                    txtCooldownTime.text = leftTime.ToString(@"mm\:ss");
                }
            }, null, true);
        } else {
            timer.Stop();
        }
    }

    private void UpdateUI() {
        int maxHeartAmount = ConfigDatabase.Instance.MaxHeart;
        int heartAmount = GameData.Inventory.GetCount(ItemID.Heart);
        bool isRegen = heartAmount < maxHeartAmount;

        if (isRegen) {

        } else {
            txtCooldownTime.text = "Full";
        }

        infinityIcon.SetActive(GameData.Inventory.IsInfinityHeart);

        if (GameData.Inventory.IsInfinityHeart) {
            TimeSpan cooldownTime = (GameData.Inventory.InfinityHeartTime - DateTime.Now);
            int totalScecond = (int)cooldownTime.TotalSeconds + 1;

            timer.Countdown(totalScecond, () => {
                TimeSpan leftTime = new TimeSpan(timer.Remaining * TimeSpan.TicksPerSecond);
                if (leftTime.TotalHours >= 1) {
                    txtCooldownTime.text = leftTime.ToString(@"hh\:mm\:ss");
                } else {
                    txtCooldownTime.text = leftTime.ToString(@"mm\:ss");
                }
            }, null, true);
        } else {
            timer.Stop();
        }
    }
}
