using UnityEditor;

public class SceneTools {
    #region Editor Scenes Menu
    public static string[] ScenePaths = { "Assets/Game/Scenes/Bootstrap.unity",
                                          "Assets/Game/Scenes/Home.unity",
                                          "Assets/Game/Scenes/Game.unity" };

    [MenuItem("Game/Play %P", false, 0)]
    private static void PlayGame() {
        OpenBootstrapScene();
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Game/Scenes/Bootstrap", false, 1)]
    private static void OpenBootstrapScene() {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePaths[0]);
    }

    [MenuItem("Game/Scenes/Home", false, 1)]
    private static void OpenSplashScene() {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePaths[1]);
    }

    [MenuItem("Game/Scenes/Game", false, 1)]
    private static void OpenGamePlayScene() {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePaths[2]);
    }
    #endregion
}