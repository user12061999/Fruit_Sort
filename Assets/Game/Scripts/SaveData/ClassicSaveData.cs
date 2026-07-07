using UnityEngine;
using HAVIGAME.SaveLoad;

[System.Serializable]
public class ClassicSaveData : SaveData {
    [SerializeField] private int levelUnlocked;
    [SerializeField] private int levelCompleted;
    [SerializeField] private int winTimes;
    [SerializeField] private int loseTimes;

    public int LevelUnlocked => levelUnlocked;
    public int LevelCompleted => levelCompleted;
    public int WinTimes => winTimes;
    public int LoseTimes => loseTimes;

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
