using HAVIGAME.SaveLoad;
using System;
using UnityEngine;

[System.Serializable]
public class DailyRewardSaveData : SaveData {
    [SerializeField] private bool isUnlocked;
    [SerializeField] private int dayUnlocked;
    [SerializeField] private long dayCompletedTicks;
    [SerializeField] private int week;

    public bool IsUnlocked => isUnlocked;
    public int DayUnlocked => dayUnlocked;
    public int Week => week;

    public DailyRewardSaveData() {
        isUnlocked = false;
        dayUnlocked = 1;
        dayCompletedTicks = 0;
        week = 1;
    }

    public void OnUpdateVersion(int oldVersion, int newVersion) {

    }

    public void Unlock() {
        if (!IsUnlocked) {
            isUnlocked = true;
            SetChanged();
        }
    }

    public int CompareDay(int day) {
        if (day < dayUnlocked) {
            return -1;
        }
        else if (day > dayUnlocked) {
            return 1;
        }
        else return 0;
    }

    public TimeSpan GetLeftTime() {
        DateTime dayCompleted = new DateTime(dayCompletedTicks);

        return dayCompleted.AddDays(1) - DateTime.Now;
    }

    public bool CanCollect(int day) {
        if (CompareDay(day) == 0) {
            DateTime dayCompleted = new DateTime(dayCompletedTicks);

            return DateTime.Today.CompareTo(dayCompleted) > 0;
        }

        return false;
    }

    public void OnRewardCollected(int day) {
        if (day == dayUnlocked) {
            UnlockNextDay();
            CompleteNextDay();
        }
    }

    private void UnlockNextDay() {
        dayUnlocked++;

        if (dayUnlocked > 7) {
            dayUnlocked = 1;
            week++;
        }
        SetChanged();
    }

    private void CompleteNextDay() {
        dayCompletedTicks = DateTime.Today.Ticks;
        SetChanged();
    }
}
