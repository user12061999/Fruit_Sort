using HAVIGAME;
using UnityEngine;

public class LevelLoader {
    private GameObject root;
    private LevelController level;
    private LoadLevelOption option;
    private States state;
    
    public States State => state;
    public LevelController Level => level;

    public GameObject Root => root;

    public LevelLoader(LoadLevelOption option) {
        this.option = option;
        this.state = States.None;

        root = new GameObject($"Level Loader");
    }

    public void Load() {
        if (state == States.None) {
            LoadLevel();
        }
        else {
            Log.Warning("[LevelLoader] The level has been loaded.");
        }
    }

    public void Destroy() {
        Object.Destroy(root);
    }

    private void LoadLevel() {
        state = States.Loading;

        LevelController levelPrefab = Resources.Load<LevelController>(option.Path);

        if (levelPrefab == null) {
            Log.Error($"[LevelLoader] Load failed! Level path: {option.Path}");
            state = States.Failed;
        }
        else {
            level = Object.Instantiate(levelPrefab, root.transform);
            state = States.Completed;
        }
    }

    public enum States {
        None = 0,
        Loading = 1,
        Completed = 2,
        Failed = 3,
        Destroyed = 4,
    }
}

[System.Serializable]
public struct LoadLevelOption {
    private const string SharedClassicLevelPath = "Levels/Level_1";

    [SerializeField] private string path;
    [SerializeField] private int level;

    public bool IsLevelTutorial => level == 0;
    public string Path => path;
    public int Level => level;


    public override string ToString() {
        return $"[LoadLevelOption] level= {level}, path= {path}";
    }

    public static LoadLevelOption Create(int level) {
        LoadLevelOption option = new LoadLevelOption { level = level, path = SharedClassicLevelPath };
        return option;
    }
}
