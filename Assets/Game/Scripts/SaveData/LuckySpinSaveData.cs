using System;
using UnityEngine;
using HAVIGAME.SaveLoad;

[System.Serializable]
public class LuckySpinSaveData : SaveData {
    public static readonly TimeSpan cooldown = new TimeSpan(12, 0, 0);

    [SerializeField] private int spinCount;
    [SerializeField] private long lastSpinTicks;

    public int SpinCount => spinCount;
    public bool HasFreeSpin {
        get {
            DateTime lastSpinTime = new DateTime(lastSpinTicks);

            return DateTime.Now.CompareTo(lastSpinTime.Add(cooldown)) > 0;
        }
    }

    public LuckySpinSaveData() {
        spinCount = 0;
        lastSpinTicks = 0;
    }

    public void ResetFreeSpin() {
        lastSpinTicks = 0;
        SetChanged();
    }

    public TimeSpan GetLeftCooldownTime() {
        DateTime lastSpinTime = new DateTime(lastSpinTicks);
        return lastSpinTime.Add(cooldown) - DateTime.Now;
    }

    public void OnFreeSpinCompleted() {
        spinCount++;
        lastSpinTicks = DateTime.Now.Ticks;
        SetChanged();
    }

    public void OnAdsSpinCompleted() {
        spinCount++;
        SetChanged();
    }
}
