using HAVIGAME;
using System;
using UnityEngine;

public class HeartRegenerator : Singleton<HeartRegenerator> {

    private Timer timer = new Timer();

    public Timer Timer =>timer;

    private void Start() {
        CheckRegen();

        EventDispatcher.AddListener<GameEvent.PlayerInventoryChanged>(OnPlayerInventoryChanged);
    }

    protected override void OnDestroy() {
        base.OnDestroy();

        EventDispatcher.RemoveListener<GameEvent.PlayerInventoryChanged>(OnPlayerInventoryChanged);
    }

    private void OnApplicationFocus(bool focus) {
        if (focus) {
            CheckRegen();
        }
    }

    private void OnPlayerInventoryChanged(GameEvent.PlayerInventoryChanged e) {
        if (e.ItemStackChange.Id == ItemID.Heart) {
            CheckRegen();
        }
    }

    private void CheckRegen() {
        int maxHeartAmount = ConfigDatabase.Instance.MaxHeart;
        int heartAmount = GameData.Inventory.GetCount(ItemID.Heart);

        if (heartAmount >= maxHeartAmount) {
            StopRegen();
        }
        else {
            StartRegen();
        }
    }

    private void StartRegen() {
        DateTime heartRegenTime = GameData.Inventory.HeartRegenTime;
        DateTime nowDateTime = DateTime.Now;

        if (heartRegenTime.CompareTo(nowDateTime) > 0) {
            heartRegenTime = nowDateTime;
            GameData.Inventory.ChangeHeartRegenTime(nowDateTime);
        }

        TimeSpan timeSpan = nowDateTime - heartRegenTime;

        int totalSeconds = (int)timeSpan.TotalSeconds;
        int heartRegenCooldown = ConfigDatabase.Instance.HeartRegenCooldown;

        if (totalSeconds >= heartRegenCooldown) {
            int maxHeartAmount = ConfigDatabase.Instance.MaxHeart;
            int heartAmount = GameData.Inventory.GetCount(ItemID.Heart);
            int regenAmount = Mathf.FloorToInt(totalSeconds / heartRegenCooldown);

            int regenAmountToAdd = Mathf.Min(maxHeartAmount - heartAmount, regenAmount);
            ItemStack regenStack = new ItemStack(ItemID.Heart, regenAmountToAdd);

            DateTime newHeartRegenTime = heartRegenTime.AddSeconds(regenAmountToAdd * heartRegenCooldown);
            GameData.Inventory.ChangeHeartRegenTime(newHeartRegenTime);
            GameData.Inventory.Add(regenStack, "regen");
          
        }
        else {
            int remainingTime = heartRegenCooldown - totalSeconds;
            timer.Countdown(remainingTime, OnHeartRegenUpdate, OnHeartRegenComplete, true);
        }
    }

    private void OnHeartRegenUpdate() {
        EventDispatcher.Dispatch(new GameEvent.HeartRegenChanged(timer.Total, timer.Elapsed, timer.Remaining));
    }

    private void OnHeartRegenComplete() {
        CheckRegen();
    }

    private void StopRegen() {
        timer.Stop();
        GameData.Inventory.ChangeHeartRegenTime(DateTime.MaxValue);

    }
    
}
