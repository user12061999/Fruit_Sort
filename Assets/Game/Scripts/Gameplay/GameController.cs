using HAVIGAME;
using System.Collections;
using MonsterLove.StateMachine;

public class GameController : Singleton<GameController> {
    private StateMachine<GameStates> stateMachine;
    private LoadLevelOption loadLevelOption;
    private LevelLoader levelLoader;
    private LevelController levelController;
    private GameParamater gameParamater;

    public override bool IsDontDestroyOnLoad => false;
    public LoadLevelOption LoadLevelOption => loadLevelOption;
    public GameStates CurrentState => stateMachine.State;
    public GameParamater GameParamater => gameParamater;
    public LevelLoader LevelLoader => levelLoader;
    public LevelController LevelController => levelController;

    protected override void OnAwake() {
        stateMachine = StateMachine<GameStates>.Initialize(this, GameStates.None);
    }

    public bool StartGame(LoadLevelOption loadLevelOption) {
        if (CurrentState == GameStates.None || CurrentState == GameStates.Destroyed) {
            Log.Debug($"[GameController] Start game with option: {loadLevelOption}");

            this.loadLevelOption = loadLevelOption;
            this.gameParamater = GameParamater.Create();
            this.levelController = null;

            stateMachine.ChangeState(GameStates.Loading);
            return true;
        }
        else {
            Log.Warning("[GameController] Start failed! Another game has been started.");
            return false;
        }
    }

    public bool SkipGame() {
        if (CurrentState != GameStates.Playing) {
            Log.Debug($"[GameController] Skip game failed! current state = {CurrentState}");
            return false;
        }
        else {
            levelController.WinLevel(true);
            Log.Debug($"[GameController] Skip game");
            return true;
        }
    }

    public bool DestroyGame() {
        GameAdvertising.TryHideBannerAd();
        if (CurrentState != GameStates.Playing) {
            Log.Debug($"[GameController] Destroy game failed! current state = {CurrentState}");
            return false;
        }
        else {
            levelController.DestroyLevel();
            Log.Debug($"[GameController] Destroy game");
            return true;
        }
    }

    private void Loading_Enter() {
        Log.Debug($"[GameController] Loading...");

        levelLoader = new LevelLoader(LoadLevelOption);
        levelLoader.Load();

        if (levelLoader.State == LevelLoader.States.Completed) {
            Log.Debug($"[GameController] Load level completed!");

            levelController = levelLoader.Level;
            levelController.Initilaze();

            stateMachine.ChangeState(GameStates.Playing);
        }
        else {
            Log.Error($"[GameController] Load level failed! Level loader state {levelLoader.State}");
        }
    }

    private IEnumerator Playing_Enter() {
        Log.Debug($"[GameController] Start Playing...");

        yield return null;

        levelController.StartLevel();
    }

    private void Destroyed_Enter() {
        Log.Debug($"[GameController] Destroyed!");

        levelLoader.Destroy();
    }
}

[System.Serializable]
public enum GameStates {
    None = 0,
    Loading,
    Playing,
    Destroyed,
}