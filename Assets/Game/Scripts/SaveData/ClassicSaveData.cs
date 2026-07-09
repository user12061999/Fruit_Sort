using System.Collections.Generic;
using UnityEngine;
using HAVIGAME.SaveLoad;

[System.Serializable]
public class ClassicSaveData : SaveData {
    [SerializeField] private int levelUnlocked;
    [SerializeField] private int levelCompleted;
    [SerializeField] private int winTimes;
    [SerializeField] private int loseTimes;
    // Sao cao nhất từng đạt của mỗi level; index = level - 1 (0 = chưa có sao).
    [SerializeField] private List<int> levelStars = new List<int>();

    public int LevelUnlocked => levelUnlocked;
    public int LevelCompleted => levelCompleted;
    public int WinTimes => winTimes;
    public int LoseTimes => loseTimes;

    public int GetLevelStars(int level) {
        int index = level - 1;
        return (index >= 0 && index < levelStars.Count) ? levelStars[index] : 0;
    }

    /// <summary>Tổng sao mọi level (dùng cho progression/chest).</summary>
    public int TotalStars {
        get {
            int total = 0;
            for (int i = 0; i < levelStars.Count; i++) total += levelStars[i];
            return total;
        }
    }

    /// <summary>Ghi sao cho level; chỉ ghi đè khi CAO HƠN kỷ lục cũ.</summary>
    public void SetLevelStars(int level, int stars) {
        if (level < 1 || stars <= 0) return;
        while (levelStars.Count < level) levelStars.Add(0);
        if (stars > levelStars[level - 1]) {
            levelStars[level - 1] = Mathf.Clamp(stars, 1, 3);
            SetChanged();
        }
    }

    public ClassicSaveData() : base() {
        levelUnlocked = 1;
        levelCompleted = 0;
        winTimes = 0;
        loseTimes = 0;
    }

    public void Dispose() {

    }

    public void OnUpdateVersion(int oldVersion, int newVersion) {

    }

    public override void OnAfterLoad() {
        base.OnAfterLoad();
        if (levelCompleted == levelUnlocked) {
            UnlockNextLevel();
        }
    }

    public bool OnLevelCompleted(int level) {
        winTimes++;
        SetChanged();

        if (level == levelUnlocked) {
            UnlockNextLevel();
            return CompleteNextLevel();
        } else {
            return false;
        }
    }

    public void OnLevelDefeat(int level) {
        loseTimes++;
        SetChanged();
    }

    private bool CompleteNextLevel() {
        int maxLevel = ConfigDatabase.Instance.MaxLevel;

        if (levelCompleted < maxLevel) {
            levelCompleted++;
            SetChanged();
            return true;
        }

        return false;
    }

    private void UnlockNextLevel() {
        int maxLevel = ConfigDatabase.Instance.MaxLevel;

        if (levelUnlocked < maxLevel) {
            levelUnlocked++;
            SetChanged();
        }
    }
}
