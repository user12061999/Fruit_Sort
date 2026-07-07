using System.Collections.Generic;

public class GameSceneController : SceneController {
    public static LoadLevelOption pendingLoadLevelOption;

    protected override void OnSceneStart() {
        GameController.Instance.StartGame(pendingLoadLevelOption);
    }
}
