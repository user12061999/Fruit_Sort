using HAVIGAME.SaveLoad;

public static class GameData {
    private static DataHolder<SaveDataInfo> info;
    private static DataHolder<PlayerSaveData> player;
    private static DataHolder<ClassicSaveData> classic;
    private static DataHolder<InventorySaveData> inventory;
    private static DataHolder<DailyRewardSaveData> dailyReward;
    private static DataHolder<LuckySpinSaveData> luckySpin;
    private static DataHolder<MissionSaveData> missions;

    public static SaveDataInfo Info => info.Data;
    public static PlayerSaveData Player => player.Data;
    public static ClassicSaveData Classic => classic.Data;
    public static InventorySaveData Inventory => inventory.Data;
    public static DailyRewardSaveData DailyReward => dailyReward.Data;
    public static LuckySpinSaveData LuckySpin => luckySpin.Data;
    public static MissionSaveData Missions => missions.Data;

    public static void Initialize(string folder = SaveLoadManager.defaultFolder) {

        info = SaveLoadManager.Create<SaveDataInfo>("info", folder);
        player = SaveLoadManager.Create<PlayerSaveData>("player", folder);
        classic = SaveLoadManager.Create<ClassicSaveData>("classic", folder);
        inventory = SaveLoadManager.Create<InventorySaveData>("inventory", folder);
        dailyReward = SaveLoadManager.Create<DailyRewardSaveData>("daily_reward", folder);
        luckySpin = SaveLoadManager.Create<LuckySpinSaveData>("lucky_spin", folder);
        missions = SaveLoadManager.Create<MissionSaveData>("missions", folder);

        if (Info.Version != SaveDataInfo.dataVersion) {
            int oldSaveDataVersion = Info.Version;
            int newSaveDataVersion = SaveDataInfo.dataVersion;

            //Update data to new version

            Info.Version = SaveDataInfo.dataVersion;
        }
    }
}