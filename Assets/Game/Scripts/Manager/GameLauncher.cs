using HAVIGAME;

[System.Serializable]
public class GameLauncher : Launcher {
    private static bool launching = false;

    public static bool Launching => launching;

    public override void Launch() {
        GameData.Initialize();
        GameAdvertising.Instance.Create();

        launching = true;
    }
}
