using System;
using HAVIGAME;
using HAVIGAME.SaveLoad;
using UnityEngine;

[System.Serializable]
public class InventorySaveData : ItemContainer, ISaveData {
    [System.NonSerialized] private bool isChanged;
    [SerializeField] private long heartRegenTimeTicks;
    [SerializeField] private long infinityHeartTimeTicks;
    public DateTime HeartRegenTime => new DateTime(heartRegenTimeTicks);
    public DateTime InfinityHeartTime => new DateTime(infinityHeartTimeTicks);
    public bool IsChanged => isChanged;
    [System.NonSerialized] private bool isLastInfinityHeart;

    public InventorySaveData() : base() {
        Add(ConfigDatabase.Instance.DefaultInventory, "default", false);

        heartRegenTimeTicks = DateTime.Now.Ticks;
        infinityHeartTimeTicks = 0;
        isChanged = true;
        isLastInfinityHeart = false;
    }

    public bool IsInfinityHeart {
        get {
            bool isInfinityHeart = infinityHeartTimeTicks > DateTime.Now.Ticks;

            if (isLastInfinityHeart != isInfinityHeart) {
                isLastInfinityHeart = isInfinityHeart;
                GameAnalytics.SetProperty("infinity_heart", isInfinityHeart ? "1" : "0");
            }

            return isInfinityHeart;
        }
    }
    public void Dispose() {

    }

    public void OnAfterLoad() {

    }

    public void OnBeforeSave() {
        isChanged = false;
    }

    protected override void OnAdded(ItemStack itemStack, string placement=null, bool raiseEventTracking = true) {
        base.OnAdded(itemStack, placement, raiseEventTracking);

        isChanged = true;

        if (raiseEventTracking) {
            ItemData data = ItemDatabase.Instance.GetDataById(itemStack.Id);
            string name = data.name.Replace(" ", "_").ToLower();

            //SetCurrentProperties(itemStack);

            GameAnalytics.LogEvent(GameAnalytics.GameEvent.Create("resource_earn")
                .Add(name, itemStack.Amount)
                .Add("position", placement));
        }

        EventDispatcher.Dispatch(new GameEvent.PlayerInventoryChanged(this, itemStack, true));
    }

    protected override void OnAdded(ItemStack[] itemStacks, string placement=null, bool raiseEventTracking = true) {
        base.OnAdded(itemStacks, placement, raiseEventTracking);

        isChanged = true;

        if (raiseEventTracking) {
            GameAnalytics.GameEvent gameEvent = GameAnalytics.GameEvent.Create("resource_earn").Add("position", placement);

            foreach (var itemStack in itemStacks) {

                ItemData data = ItemDatabase.Instance.GetDataById(itemStack.Id);

                string name = data.name.Replace(" ", "_").ToLower();

                //SetCurrentProperties(itemStack);

                gameEvent.Add($"resouce_{name}", itemStack.Amount.ToString());
            }

            GameAnalytics.LogEvent(gameEvent);
        }

        foreach (var itemStack in itemStacks) {
            EventDispatcher.Dispatch(new GameEvent.PlayerInventoryChanged(this, itemStack, true));
        }
    }

    protected override void OnRemoved(ItemStack itemStack, string placement=null, bool raiseEventTracking = true) {
        base.OnRemoved(itemStack, placement, raiseEventTracking);

        isChanged = true;

        if (raiseEventTracking) {
            ItemData data = ItemDatabase.Instance.GetDataById(itemStack.Id);
            string name = data.name.Replace(" ", "_").ToLower();

            //SetCurrentProperties(itemStack);

            GameAnalytics.LogEvent(GameAnalytics.GameEvent.Create("resource_spend")
                .Add(name, itemStack.Amount)
                .Add("position", placement));
        }

        EventDispatcher.Dispatch(new GameEvent.PlayerInventoryChanged(this, itemStack, false));
    }

    protected override void OnRemoved(ItemStack[] itemStacks, string placement=null, bool raiseEventTracking = true) {
        base.OnAdded(itemStacks, placement, raiseEventTracking);

        isChanged = true;

        if (raiseEventTracking) {
            GameAnalytics.GameEvent gameEvent = GameAnalytics.GameEvent.Create("resource_spend").Add("position", placement);

            foreach (var itemStack in itemStacks) {

                ItemData data = ItemDatabase.Instance.GetDataById(itemStack.Id);

                string name = data.name.Replace(" ", "_").ToLower();

                //SetCurrentProperties(itemStack);

                gameEvent.Add($"resouce_{name}", itemStack.Amount.ToString());
            }

            GameAnalytics.LogEvent(gameEvent);
        }

        foreach (var itemStack in itemStacks) {
            EventDispatcher.Dispatch(new GameEvent.PlayerInventoryChanged(this, itemStack, false));
        }
    }
    
    public void ChangeHeartRegenTime(DateTime dateTime) {
        heartRegenTimeTicks = dateTime.Ticks;
        isChanged = true;
    }
    protected override void OnCleared() {
        base.OnCleared();

        isChanged = true;
    }

    public void OnCreate() {

    }

    public void OnDestroy() {

    }
}
