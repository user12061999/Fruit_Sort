using HAVIGAME;
using UnityEngine;
using SimpleJSON;
using System.Collections.Generic;

public abstract class Mission : ScriptableObject, IIdentify<string> {
    public static readonly IComparer<Mission> comparer = new MissionComparer();

    [SerializeField] private string fullName;
    [SerializeField] private string shortName;
    [SerializeField, SpriteField] private Sprite icon;
    [SerializeField] private string id;
    [SerializeField] private MissionTypes type;
    [SerializeField, TextArea(3, 5)] private string description;
    [SerializeField] private int requireProgress = 1;
    [SerializeField] private int difficult = 1;
    [SerializeField] private ItemStack[] rewards;
    [SerializeField] private MissionCondition[] conditions;

    public string Id => id;
    public MissionTypes Type => type;
    public Sprite Icon => icon;
    public string FullName => fullName;
    public string ShortName => shortName;
    public string Description => description;
    public int CurrentProgress => currentProgress;
    public int RequireProgress => requireProgress;
    public int Difficult => difficult;
    public MissionStates State => state;
    public ItemStack[] Rewards => rewards;
    public bool IsActive {
        get {
            if (conditions != null) {
                foreach (var item in conditions) {
                    if (!item.CheckCondition(this)) return false;
                }
            }
            return true;
        }
    }


    [System.NonSerialized] protected MissionStates state = MissionStates.None;
    [System.NonSerialized] protected int currentProgress = 0;

    public void Reset() {
        if (state != MissionStates.None) {
            OnReset();
            Log.Debug($"[Mission] {name}: Reset.");
        } else {
            Log.Warning($"[Mission] {name}: Reset failed! Mission is running.");
        }
    }

    public void Start() {
        if (state == MissionStates.None) {
            state = MissionStates.Running;
            OnStarted();
            Log.Debug($"[Mission] {name}: Start.");
        } else {
            Log.Warning($"[Mission] {name}: Start failed! Mission is running.");
        }
    }

    public void Cancel(bool complete = false) {
        if (state == MissionStates.Running) {
            if (complete) {
                Complete();
            } else {
                Fail();
            }
        } else {
            Log.Warning($"[Mission] {name}: Cancel failed! Mission is not running.");
        }
    }

    public void Claim() {
        if (state == MissionStates.Completed) {
            state = MissionStates.Claimed;
            Log.Debug($"[Mission] {name}: Claim.");
        } else {
            Log.Warning($"[Mission] {name}: Claim failed! Mission is not completed.");
        }
    }

    protected void Complete() {
        state = MissionStates.Completed;

        OnCompleted();
        Log.Debug($"[Mission] {name}: Completed.");

        OnFinshed();
        Log.Debug($"[Mission] {name}: Finished.");
    }

    protected void Fail() {
        state = MissionStates.Failed;

        OnFailed();
        Log.Debug($"[Mission] {name}: Failed.");

        OnFinshed();
        Log.Debug($"[Mission] {name}: Finished.");
    }

    protected void AddProgress(int value) {
        if (state == MissionStates.Running && IsActive) {
            currentProgress = Mathf.Min(CurrentProgress + value, RequireProgress);

            Log.Debug($"[Mission] {name}: Update progress {CurrentProgress}/{RequireProgress}");

            if (CurrentProgress >= RequireProgress) {
                Complete();
            }
        }
    }

    protected void SetProgress(int value) {
        if (state == MissionStates.Running && IsActive) {
            currentProgress = Mathf.Min(value, RequireProgress);

            Log.Debug($"[Mission] {name}: Update progress {CurrentProgress}/{RequireProgress}");

            if (CurrentProgress >= RequireProgress) {
                Complete();
            }
        }
    }

    protected virtual void OnReset() {
        state = MissionStates.None;
        currentProgress = 0;
    }
    protected virtual void OnStarted() { }
    protected virtual void OnFailed() { }
    protected virtual void OnCompleted() {
        EventDispatcher.Dispatch(new GameEvent.MissionCompleted(Id));
    }
    protected virtual void OnFinshed() { }

    public virtual JSONNode ToJSON() {
        JSONNode node = new JSONObject();
        node.Add("state", (int)state);
        node.Add("progress", currentProgress);
        return node;
    }
    public virtual void FromJSON(JSONNode node) {
        state = (MissionStates)node["state"].AsInt;
        currentProgress = node["progress"];

        if (state == MissionStates.Running) state = MissionStates.None;
    }

    private class MissionComparer : IComparer<Mission> {
        public int Compare(Mission x, Mission y) {
            if (x.state == y.state) {
                return 0;
            } else {
                int xSortingOrder = GetSortingOrder(x);
                int ySortingOrder = GetSortingOrder(y);

                return ySortingOrder.CompareTo(xSortingOrder);
            }
        }

        private int GetSortingOrder(Mission mission) {
            switch (mission.state) {
                case MissionStates.None:
                    return 1;
                case MissionStates.Running:
                    return 4;
                case MissionStates.Failed:
                    return 3;
                case MissionStates.Completed:
                    return 5;
                case MissionStates.Claimed:
                    return 2;
                default:
                    return 0;
            }
        }
    }
}

public abstract class Mission<T> : Mission where T : IEventArgs {
    [System.NonSerialized] protected bool registed;

    protected override void OnReset() {
        base.OnReset();

        if (registed) {
            Unregister();
        }
    }

    protected override void OnStarted() {
        if (!registed && state == MissionStates.Running) {
            Register();
        }
    }

    protected override void OnFinshed() {
        if (registed) {
            Unregister();
        }
    }

    protected virtual void Register() {
        EventDispatcher.AddListener<T>(OnUpdate);
        registed = true;
        Log.Debug($"[Mission] {name}: Register event dispatcher.");
    }

    protected virtual void Unregister() {
        EventDispatcher.RemoveListener<T>(OnUpdate);
        registed = false;
        Log.Debug($"[Mission] {name}: Unregister event dispatcher.");
    }

    protected abstract void OnUpdate(T args);
}

[System.Serializable]
public enum MissionStates {
    None,
    Running,
    Failed,
    Completed,
    Claimed,
}

[System.Serializable]
public enum MissionTypes {
    Daily,
    Weekly,
    Achievement,
    Christmass,
}