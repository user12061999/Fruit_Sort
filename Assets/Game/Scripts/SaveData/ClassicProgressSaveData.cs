using System.Collections.Generic;
using UnityEngine;
using HAVIGAME.SaveLoad;

/// <summary>
/// Snapshot tiến trình của MỘT level classic đang chơi dở.
/// - Sau mỗi action (click spawner / nhả bucket) controller gọi SaveLoadManager.Save(),
///   OnBeforeSave của class này tự chụp trạng thái sống qua <see cref="ICaptureProvider"/>.
/// - Kill app -> lần đăng nhập sau vào lại level đó sẽ được khôi phục thay vì chơi lại từ đầu.
/// - Thắng / thua level -> gọi <see cref="Clear"/> để xoá snapshot.
/// </summary>
[System.Serializable]
public class ClassicProgressSaveData : SaveData {

    /// <summary>Nguồn chụp snapshot (ClassicLevelController đăng ký khi level đang chạy).</summary>
    public interface ICaptureProvider {
        /// <summary>Ghi trạng thái hiện tại vào data. Trả về false nếu không có gì để lưu.</summary>
        bool CaptureTo(ClassicProgressSaveData data);
    }

    [System.Serializable]
    public class BucketState {
        public int index;                            // index dựng trong LevelData.buckets
        public bool done;                            // đã đầy (hoặc đã bị worker dọn khỏi scene)
        public List<int> colors = new List<int>();   // colorId các dot ĐANG nằm trong giỏ, theo thứ tự
    }

    [System.Serializable]
    public class SpawnerState {
        public int dotsLeft;                         // số dot còn lại trong gói
    }

    [System.Serializable]
    public class SwitchState {
        public int conveyorIndex;                    // index băng trong thứ tự dựng của LevelBuilder
        public int activeIndex;                      // nhánh đang chọn của ConveyorSwitch
    }

    [System.Serializable]
    public class DotSnapshot {
        public int colorId;
        public int conveyorIndex = -1;               // >=0: đang trên băng chuyền; -1: đang bay
        public float progress;                       // beltProgress (khi trên băng)
        public float lateral;                        // lateralOffset (khi trên băng)
        public Vector2 position;                     // vị trí world (khi đang bay)
        public Vector2 velocity;                     // vận tốc (khi đang bay)
    }

    private static ICaptureProvider provider;

    public static void SetCaptureProvider(ICaptureProvider p) {
        provider = p;
    }

    public static void RemoveCaptureProvider(ICaptureProvider p) {
        if (ReferenceEquals(provider, p)) provider = null;
    }

    [SerializeField] private bool hasProgress;
    [SerializeField] private int level;
    [SerializeField] private int configHash;   // LevelData.ComputeConfigHash lúc chụp
    [SerializeField] private int movesLeft;
    [SerializeField] private float timeLeft;
    [SerializeField] private int score;
    [SerializeField] private int bucketCount;
    [SerializeField] private int spawnerCount;
    [SerializeField] private int conveyorCount;
    [SerializeField] private int obstacleCount;
    [SerializeField] private List<BucketState> buckets = new List<BucketState>();
    [SerializeField] private List<SpawnerState> spawners = new List<SpawnerState>();
    [SerializeField] private List<DotSnapshot> dots = new List<DotSnapshot>();
    // Index (theo tên "Obstacle_{i}" LevelBuilder đặt) của các vật cản ĐÃ VỠ trước khi save.
    // Vật cản còn sống không cần state: số dot đang đè là per-frame, tự tính lại từ dot khôi phục.
    [SerializeField] private List<int> brokenObstacles = new List<int>();
    // Trạng thái các ConveyorSwitch (nhánh đang chọn) — chỉ băng nào có switch mới có entry.
    [SerializeField] private List<SwitchState> switches = new List<SwitchState>();

    public bool HasProgress => hasProgress;
    public int Level => level;
    public int ConfigHash => configHash;
    public int MovesLeft => movesLeft;
    public float TimeLeft => timeLeft;
    public int Score => score;
    public int BucketCount => bucketCount;
    public int SpawnerCount => spawnerCount;
    public int ConveyorCount => conveyorCount;
    public int ObstacleCount => obstacleCount;
    public IReadOnlyList<BucketState> Buckets => buckets;
    public IReadOnlyList<SpawnerState> Spawners => spawners;
    public IReadOnlyList<DotSnapshot> Dots => dots;
    public IReadOnlyList<int> BrokenObstacles => brokenObstacles;
    public IReadOnlyList<SwitchState> Switches => switches;

    // Đang trong level (có provider) -> luôn coi là changed để mọi auto-save
    // (pause / quit / sau action) đều chụp lại trạng thái mới nhất.
    public override bool IsChanged => base.IsChanged || provider != null;

    public override void OnBeforeSave() {
        if (provider != null) provider.CaptureTo(this);
        base.OnBeforeSave();
    }

    /// <summary>Bắt đầu một snapshot mới: ghi thông số chung và xoá dữ liệu cũ.</summary>
    public void BeginCapture(int level, int configHash, int movesLeft, float timeLeft, int score,
                             int bucketCount, int spawnerCount, int conveyorCount, int obstacleCount) {
        this.level = level;
        this.configHash = configHash;
        this.movesLeft = movesLeft;
        this.timeLeft = timeLeft;
        this.score = score;
        this.bucketCount = bucketCount;
        this.spawnerCount = spawnerCount;
        this.conveyorCount = conveyorCount;
        this.obstacleCount = obstacleCount;
        buckets.Clear();
        spawners.Clear();
        dots.Clear();
        brokenObstacles.Clear();
        switches.Clear();
        hasProgress = true;
        SetChanged();
    }

    public void AddBrokenObstacle(int index) {
        brokenObstacles.Add(index);
    }

    public void AddSwitchState(int conveyorIndex, int activeIndex) {
        switches.Add(new SwitchState { conveyorIndex = conveyorIndex, activeIndex = activeIndex });
    }

    public void AddBucketState(int index, bool done, List<int> containedColors) {
        BucketState state = new BucketState { index = index, done = done };
        if (containedColors != null) state.colors.AddRange(containedColors);
        buckets.Add(state);
    }

    public void AddSpawnerState(int dotsLeft) {
        spawners.Add(new SpawnerState { dotsLeft = dotsLeft });
    }

    public void AddBeltDot(int colorId, int conveyorIndex, float progress, float lateral) {
        dots.Add(new DotSnapshot {
            colorId = colorId,
            conveyorIndex = conveyorIndex,
            progress = progress,
            lateral = lateral,
        });
    }

    public void AddFlyingDot(int colorId, Vector2 position, Vector2 velocity) {
        dots.Add(new DotSnapshot {
            colorId = colorId,
            conveyorIndex = -1,
            position = position,
            velocity = velocity,
        });
    }

    /// <summary>Xoá snapshot (gọi khi thắng / thua level).</summary>
    public void Clear() {
        hasProgress = false;
        level = 0;
        configHash = 0;
        movesLeft = 0;
        timeLeft = 0f;
        score = 0;
        bucketCount = 0;
        spawnerCount = 0;
        conveyorCount = 0;
        obstacleCount = 0;
        buckets.Clear();
        spawners.Clear();
        dots.Clear();
        brokenObstacles.Clear();
        switches.Clear();
        SetChanged();
    }
}
