using HAVIGAME;
using HAVIGAME.SaveLoad;

public class MissionManager : Singleton<MissionManager> {

    protected override void OnAwake() {
        base.OnAwake();

        SaveLoadManager.onBeforeSave += SaveLoadManager_onBeforeSave;

        StartMissons();
    }


    protected override void OnDestroy() {
        base.OnDestroy();

        SaveLoadManager.onBeforeSave -= SaveLoadManager_onBeforeSave;
    }

    private void SaveLoadManager_onBeforeSave() {
        MissionDatabase.Instance.SaveMissions();
    }

    private void StartMissons() {
        MissionDatabase.Instance.StartMissions();
    }
}
