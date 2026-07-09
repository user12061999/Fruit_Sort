using DG.Tweening;
using HAVIGAME;
using HAVIGAME.Scenes;
using HAVIGAME.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassicLevelController : LevelController, ClassicProgressSaveData.ICaptureProvider
{
    //singleton
    public static ClassicLevelController instance;

    [SerializeField] protected LevelTimer timer;
    [SerializeField] protected LevelGenerator generator;

    [Header("[Level Data]")]
    [Tooltip("Data theo thứ tự level 1, 2, 3...")]
    [SerializeField] private FruitSort.LevelData[] levels;


    protected ParticleSystem highlightVFX;
    protected GamePanel gamePanel;
    private int boosterUsed;
    protected bool isUsingBooster;
    protected bool isResolving;
    protected float startedTime;
    protected int totalMove;
    protected bool isWon = false;
    protected Dictionary<int, int> dictBooster;
    private int countBuyTime;
    private GameObject loadedLevelRoot;
    private int totalBuckets;
    private int filledBuckets;
    private bool isLost;
    private bool isWaitingForFirstInteraction;
    private FruitSort.GamePlayManager gamePlayManager;
    private int currentLevelNumber;
    private bool resumedFromSave;

    public GamePanel GamePanels => gamePanel;
    public LevelTimer Timer => timer;
    public bool IsResolving => isResolving;
    public bool IsUsingBooster => isUsingBooster;
    public bool IsCountdownPaused => timer.IsPaused;
    public int BoosterUsed => boosterUsed;
 
    public int CountBuyTime => countBuyTime;
    public FruitSort.LevelData CurrentLevelData { get; private set; }
    public GameObject LoadedLevelRoot => loadedLevelRoot;

    public int TotalMove
    {
        get { return totalMove; }
        set { totalMove = value; }
    }

    public void OnBlockShapeChange(GameEvent.DestroyBlockShape e)
    {
        Debug.Log("OnBlockShapeChange");
        CheckWinLose();
    }
    [ContextMenu("OnCreate")]
    public void OnCreate()
    {
        UpdateCameraToLoadedLevel();
    }
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void Start()
    {
        EventDispatcher.AddListener<GameEvent.DestroyBlockShape>(OnBlockShapeChange);
        FruitSort.Bucket.OnBucketFull += HandleBucketFull;
        FruitSort.GamePlayManager.onInteractionRecorded += HandleInteractionRecorded;
    }

    private void OnDestroy()
    {
        EventDispatcher.RemoveListener<GameEvent.DestroyBlockShape>(OnBlockShapeChange);
        FruitSort.Bucket.OnBucketFull -= HandleBucketFull;
        FruitSort.GamePlayManager.onInteractionRecorded -= HandleInteractionRecorded;
        ClassicProgressSaveData.RemoveCaptureProvider(this);
        UnbindGamePlayManagerLose();

        if (instance == this)
        {
            instance = null;
        }

    }

    #region OVERRIDE

    public override void Initilaze(LoadLevelOption option, LevelController parent)
    {
        base.Initilaze(option, parent);

        InitializeLevel(option.Level);
    }

    public override void Initilaze()
    {
        base.Initilaze();

        InitializeLevel(GameController.Instance.LoadLevelOption.Level);
    }

    private void InitializeLevel(int level)
    {
        isResolving = false;
        isWon = false;
        isLost = false;
        resumedFromSave = false;
        currentLevelNumber = level;
        dictBooster = new Dictionary<int, int>();

        if (LoadLevelData(level))
        {
            // Có tiến trình lưu dở của đúng level này -> khôi phục để chơi tiếp.
            TryRestoreProgress(level);
            // Đăng ký làm nguồn chụp snapshot: mọi SaveLoadManager.Save() (sau action,
            // khi app pause/quit) sẽ tự chụp trạng thái sống qua CaptureTo.
            ClassicProgressSaveData.SetCaptureProvider(this);
        }

        CheckHideTutorial();
    }

    public FruitSort.LevelData GetLevelData(int level)
    {
        int index = level - 1;
        if (levels == null || index < 0 || index >= levels.Length)
        {
            return null;
        }

        return levels[index];
    }

    public bool LoadLevelData(int level)
    {
        FruitSort.LevelData data = GetLevelData(level);
        if (data == null)
        {
            Debug.LogError($"[ClassicLevelController] Missing data for level {level}.", this);
            return false;
        }

        if (loadedLevelRoot != null)
        {
            Destroy(loadedLevelRoot);
        }

        loadedLevelRoot = FruitSort.LevelBuilder.Build(data);
        if (loadedLevelRoot == null)
        {
            return false;
        }

        loadedLevelRoot.transform.SetParent(transform, true);
        CurrentLevelData = data;
        BindGamePlayManager(data);

        totalBuckets = loadedLevelRoot.GetComponentsInChildren<FruitSort.Bucket>(true).Length;
        filledBuckets = 0;

        UpdateCameraToLoadedLevel();

        if (generator != null)
        {
            generator.Duration = Mathf.CeilToInt(Mathf.Max(0f, data.timeLimit));
        }

        return true;
    }

    private void BindGamePlayManager(FruitSort.LevelData data)
    {
        UnbindGamePlayManagerLose();

        gamePlayManager = FruitSort.GamePlayManager.Instance;
        if (gamePlayManager == null)
        {
            gamePlayManager = FindAnyObjectByType<FruitSort.GamePlayManager>();
        }

        if (gamePlayManager == null)
        {
            return;
        }

        gamePlayManager.ConfigureFromLevel(data);
        gamePlayManager.SetTimeCounting(false);
        gamePlayManager.onLose.RemoveListener(HandleGamePlayLose);
        gamePlayManager.onLose.AddListener(HandleGamePlayLose);
    }

    private void UnbindGamePlayManagerLose()
    {
        if (gamePlayManager == null)
        {
            return;
        }

        gamePlayManager.onLose.RemoveListener(HandleGamePlayLose);
        gamePlayManager = null;
    }

    private void HandleGamePlayLose()
    {
        LoseLevel();
    }

    private void HandleBucketFull(FruitSort.Bucket bucket)
    {
        if (isWon || bucket == null || loadedLevelRoot == null ||
            !bucket.transform.IsChildOf(loadedLevelRoot.transform))
        {
            return;
        }

        filledBuckets++;
        if (totalBuckets > 0 && filledBuckets >= totalBuckets)
        {
            WinLevel();
        }
    }

    public override void StartLevel()
    {
        base.StartLevel();
    }

    protected override void OnStartLevel()
    {
        base.OnStartLevel();
        UpdateCameraToLoadedLevel();
        startedTime = Time.time;
        TotalMove = 0;
        // Resume level chơi dở -> không trừ tim lần nữa (đã trừ khi bắt đầu lượt gốc).
        if (!resumedFromSave) GameData.Inventory.Remove(new ItemStack(ItemID.Heart, 1));
        gamePanel = UIManager.Instance.Push<GamePanel>();
        gamePanel.SetCountdownTime(generator.Duration);
        gamePanel.Interactable = true;
        countBuyTime = 0;
        GameAdvertising.TryShowBannerAd(GameAdvertising.GameAdPosition.BottomCenter);
        int seconds = generator.Duration;
        if (gamePlayManager != null)
        {
            gamePlayManager.SetTimeCounting(false);
        }
        if (seconds > 0)
        {
            StartCountdown(seconds);
            PauseCountdown(false);
            isWaitingForFirstInteraction = true;
        }
        else
        {
            timer.Stop();
            isWaitingForFirstInteraction = false;
        }
        if (GameData.Classic.LevelUnlocked >= 3)
        {
            StartShowInterstitialAd();
        }
    }
    private void StartShowInterstitialAd()
    {
        StopShowInterstitialAd();

        float cappingTime = GameRemoteConfig.InterstitialAdCappingTime;
        if (cappingTime > 0) intersitialAdCoroutine = StartCoroutine(IEShowIntersitialAd(GameRemoteConfig.InterstitialAdCappingTime));
    }
    /*public void SetInputSource(PlayerInputHandler playerInputHandler)
    {
        playerInputHandler.SetListener(this);
        playerInputHandler.active = true;
    }*/

    [ContextMenu("Pause Countdown")]

    private void HandleLevelStart()
    {

    }

    private void UpdateCameraToLoadedLevel()
    {
        if (loadedLevelRoot == null || CameraController.Instance == null)
        {
            return;
        }

        Bounds levelBounds = CalculateLoadedLevelBounds();
        CameraController.Instance.UpdateCamera(levelBounds);
    }

    private Bounds CalculateLoadedLevelBounds()
    {
        Renderer[] renderers = loadedLevelRoot.GetComponentsInChildren<Renderer>(true);
        bool hasRendererBounds = false;
        Bounds bounds = default;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer currentRenderer = renderers[i];
            if (currentRenderer == null || !currentRenderer.enabled)
            {
                continue;
            }

            if (!hasRendererBounds)
            {
                bounds = currentRenderer.bounds;
                hasRendererBounds = true;
                continue;
            }

            bounds.Encapsulate(currentRenderer.bounds);
        }

        if (hasRendererBounds)
        {
            return bounds;
        }

        Transform[] children = loadedLevelRoot.GetComponentsInChildren<Transform>(true);
        Bounds fallbackBounds = new Bounds(loadedLevelRoot.transform.position, Vector3.zero);
        for (int i = 0; i < children.Length; i++)
        {
            fallbackBounds.Encapsulate(children[i].position);
        }

        return fallbackBounds;
    }

    public override void WinLevel(bool isWinBySkip = false)
    {
        base.WinLevel();
    }

    protected override void OnWinLevel(bool isWinBySkip = false)
    {
        base.OnWinLevel(isWinBySkip);

        isWon = true;
        ClearSavedProgress();
        timer.Stop();
        gamePanel.Interactable = false;
        Time.timeScale = 1;
        int heartAmount = GameData.Inventory.GetCount(ItemID.Heart);
        if (heartAmount<ConfigDatabase.Instance.MaxHeart)
        {
            GameData.Inventory.Add(new ItemStack(ItemID.Heart,1));
        }
        
        StopShowInterstitialAd();
        DOVirtual.DelayedCall(1.5f, () =>
        {
            gamePanel.Interactable = true;
            GameData.Classic.OnLevelCompleted(GameController.Instance.LoadLevelOption.Level);
            WinPanel winPanel = UIManager.Instance.Push<WinPanel>();

            //winPanel.SetRewards(new ItemStack[] { new ItemStack(ItemID.Coin, ConfigDatabase.Instance.CoinWin) });

            winPanel.SetCoin(new ItemStack(ItemID.Coin, ConfigDatabase.Instance.CoinWin));

            /*if (GameData.Classic.LevelUnlocked == 2)
            {
                GameSceneController.pendingLoadLevelOption = LoadLevelOption.Create(GameData.Classic.LevelUnlocked);
                ScenesManager.Instance.LoadSceneAsyn(GameScene.ByIndex.Game);
            }
            else
            {
                WinPanel winPanel = UIManager.Instance.Push<WinPanel>();
                //winPanel.SetStarRewards(new ItemStack(ItemID.Star, totalStarEarned));
                //
            }*/


        });
    }

    public override void LoseLevel()
    {
        base.LoseLevel();
    }

    protected override void OnLoseLevel()
    {
        base.OnLoseLevel();
        if (isWon || isLost) return;
        isLost = true;
        ClearSavedProgress();
        if (timer != null) timer.Pause();
        if (gamePanel != null) gamePanel.Interactable = false;
        CheckHideTutorial();
        StopShowInterstitialAd();
        Time.timeScale = 1;

        countBuyTime++;

        DOVirtual.DelayedCall(1.5f, () =>
        {
            if (isWon) return;
            if (gamePanel != null) gamePanel.Interactable = true;
            LosePanel losePanel = UIManager.Instance.Push<LosePanel>();
            losePanel.SetCoin(new ItemStack(ItemID.Coin, 250 * countBuyTime));
            losePanel.CheckButton();
        });


    }

    public int GetBoosterUsed(int boosterId)
    {
        if (!dictBooster.ContainsKey(boosterId))
            return 0;
        return dictBooster[boosterId];
    }

    public override void DestroyLevel()
    {
        base.DestroyLevel();

    }

    protected override void OnDestroyLevel()
    {
        base.OnDestroyLevel();

        ClassicProgressSaveData.RemoveCaptureProvider(this);
        timer.Stop();
        UnbindGamePlayManagerLose();

        if (highlightVFX != null)
        {
            highlightVFX.Recycle();
            highlightVFX = null;
        }

        CheckHideTutorial();
        StopShowInterstitialAd();

        Time.timeScale = 1;

        if (loadedLevelRoot != null)
        {
            Destroy(loadedLevelRoot);
            loadedLevelRoot = null;
            CurrentLevelData = null;
        }

    }

    #endregion

    #region TUTORIAL CHECK

    private void CheckShowTutorial(float delay = 1)
    {

    }

    private void CheckHideTutorial()
    {

    }

    #endregion

    #region INPUT

    public void OnPointerDown(Vector3 screenPoint)
    {
        MouseDown(screenPoint, InputSource.Player);
    }

    public void OnPointerMove(Vector3 screenPoint)
    {
        MouseMove(screenPoint, InputSource.Player);
    }

    public void OnPointerUp(Vector3 screenPoint)
    {
        MouseUp(screenPoint, InputSource.Player);
    }

    public void OnPointerClick(Vector3 screenPoint)
    {

    }

    public void MouseDown(Vector3 screenPoint, InputSource inputSource)
    {
        switch (inputSource)
        {
            case InputSource.Player:
                CheckHideTutorial();
                break;
            case InputSource.Tutorial:
                break;
            default:
                break;
        }


        if (IsUsingBooster) return;


    }

    public void MouseMove(Vector3 screenPoint, InputSource inputSource)
    {

    }

    public void MouseUp(Vector3 screenPoint, InputSource inputSource)
    {

    }

    #endregion

    #region CHEAT

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            WinLevel();
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            LoseLevel();
        }
    }
#endif

    #endregion

    private void StartCountdown(int seconds)
    {

        timer.Countdown(seconds, LoseLevel);
    }

    public void StartCountdownOnFirstInteraction()
    {
        if (!isWaitingForFirstInteraction)
        {
            return;
        }

        isWaitingForFirstInteraction = false;
        ResumeCountdown(false);

        if (gamePlayManager != null)
        {
            gamePlayManager.SetTimeCounting(true);
        }
    }


    



    protected virtual void CheckWinLose()
    {
        /*int remainingBlocks = gridManager.BlockParent.childCount; // Assuming blocks are children of GridManager
        /*int remainingBlocks = gridManager.BlockParent.childCount; // Assuming blocks are children of GridManager
        Debug.Log("check win lose: " + remainingBlocks);
        if (remainingBlocks == 0)
        {
            WinLevel();
            return;
        }*/

        // Check Lose Condition
        if (TotalMove > 100) // Example thresholds
        {
            LoseLevel();
        }
    }

    protected bool IsAllCellFull()
    {


        return true;
    }

    protected bool IsAllCellEmpty()
    {


        return true;
    }

    public void OnFocus(bool isFocus)
    {
        /*if (!isFocus && draggingSlot != null) {

            draggingSlot.EndMove(draggingSlot.DragTarget.position);

            if (highlightVFX != null) {
                highlightVFX.Recycle();
                highlightVFX = null;
            }

            highlightSlot = null;
            draggingSlot = null;

            CheckHideTutorial();
        } else {
            CheckShowTutorial();
        }


        if (isFocus) {
            ResumeCountdown(false);
        } else {
            PauseCountdown(false);
        }*/
    }

    public void FillElements()
    {

    }


    protected void DestroyElements()
    {

    }




    [ContextMenu("Pause Countdown")]
    public virtual void PauseCountdown(bool nofity = true)
    {

        timer.Pause();

        if (nofity)
        {
            EventDispatcher.Dispatch(new GameEvent.LevelCountdownChanged(true));
        }
    }


    [ContextMenu("Resume Countdown")]
    public virtual void ResumeCountdown(bool nofity = true)
    {
        timer.Resume();

        if (nofity)
        {
            EventDispatcher.Dispatch(new GameEvent.LevelCountdownChanged(false));
        }
    }


    [ContextMenu("Add 60s")]
    public virtual void Add60Seconds()
    {
        timer.Add(60);
    }

    public virtual void AddMoreSeconds(int amount)
    {
        // Mua thêm giờ để chơi tiếp sau khi thua -> mở lại cache tiến trình.
        isLost = false;
        ClassicProgressSaveData.SetCaptureProvider(this);
        // GamePlayManager cũng phải revive, nếu không state kẹt Lost -> CanUseMove = false.
        if (gamePlayManager != null) gamePlayManager.ReviveWithTime(amount);
        timer.AddTime(amount);
        timer.Resume();
    }

    /// <summary>Mua thêm lượt move để chơi tiếp sau khi thua do hết move.</summary>
    public virtual void AddMoreMoves(int amount)
    {
        isLost = false;
        ClassicProgressSaveData.SetCaptureProvider(this);
        if (gamePlayManager != null) gamePlayManager.ReviveWithMoves(amount);
        // Thua do hết move đã pause đồng hồ -> chạy tiếp nếu level có giới hạn giờ.
        if (CurrentLevelData != null && CurrentLevelData.timeLimit > 0f) timer.Resume();
    }

    /// <summary>
    /// Đóng băng gameplay khi có popup đè lên GamePanel: dừng đồng hồ
    /// (LevelTimer chạy ignoreTimeScale nên phải Pause tường minh) và dừng mọi chuyển động.
    /// </summary>
    public void PauseGameplay()
    {
        Time.timeScale = 0f;
        if (timer != null) timer.Pause();
        if (gamePlayManager != null) gamePlayManager.SetTimeCounting(false);
    }

    /// <summary>Chạy lại gameplay khi popup trên cùng đóng và GamePanel trở lại top.</summary>
    public void ResumeGameplay()
    {
        Time.timeScale = 1f;
        if (isWon || isLost) return;

        if (!isWaitingForFirstInteraction)
        {
            timer.Resume();
            if (gamePlayManager != null && gamePlayManager.HasStartedInteraction)
            {
                gamePlayManager.SetTimeCounting(true);
            }
        }
    }


    [ContextMenu("Double Stars")]
    public virtual void DoubleStars()
    {

    }

    [System.Serializable]
    public class LevelInfomation
    {
        private float startTime;
        private float finishTime;
        private int reslovedElements;
        private int currentStar;
        private int currentCombo;
        private int highestCombo;
        private float lastTimeResloved;
        private bool doubleStarEnabled;
        private bool doubleAllEnabled;

        public float StartTime => startTime;
        public float FinishTime => finishTime;
        public int CurrentStar => currentStar;
        public int CurrentCombo => currentCombo;
        public int HighestCombo => highestCombo;
        public bool IsCombing => Time.unscaledTime < ComboTime;
        public int ReslovedElements => reslovedElements;
        public float LastTimeResloved => lastTimeResloved;
        public float ComboTime => lastTimeResloved + GetCurrentComboTime(currentCombo);
        public bool DoubleStarEnabled => doubleStarEnabled;
        public bool DoubleAllEnabled => doubleAllEnabled;

        public LevelInfomation()
        {
            startTime = 0;
            finishTime = 0;
            reslovedElements = 0;
            currentStar = 0;
            currentCombo = 0;
            highestCombo = 0;
            lastTimeResloved = 0;
            doubleStarEnabled = false;
        }

        public LevelInfomation(bool hasDoubleAllBooster) : this()
        {
            this.doubleAllEnabled = hasDoubleAllBooster;
        }

        public void OnDoubleStar()
        {
            doubleStarEnabled = true;

        }

        public void OnStarted()
        {
            startTime = Time.unscaledTime;
        }

        public void OnFinished()
        {
            finishTime = Time.unscaledTime;
        }

        public void OnResolved()
        {
            if (IsCombing)
            {
                currentCombo++;
            }
            else
            {
                currentCombo = 1;
            }

            if (currentCombo > highestCombo)
            {
                highestCombo = currentCombo;
            }

            int starBonus = GetBonus(currentCombo);

            if (DoubleStarEnabled) starBonus *= 2;

            currentStar += starBonus;

            lastTimeResloved = Time.unscaledTime;

        }

        public void ResetCombo()
        {
            currentCombo = 0;
            lastTimeResloved = 0;
        }

        private int GetCurrentComboTime(int combo)
        {
            switch (combo)
            {
                case 1:
                    return 25;
                case 2:
                    return 20;
                case 3:
                    return 15;
                case 4:
                case 5:
                case 6:
                    return 10;
                case 7:
                case 8:
                case 9:
                    return 7;
                case 10:
                case 11:
                case 12:
                    return 5;
                case 13:
                case 14:
                case 15:
                    return 3;
                case 16:
                case 17:
                    return 2;
                default:
                    return 1;
            }
        }

        private int GetBonus(int combo)
        {
            return 1 + combo / 3;
        }
    }

    public enum InputSource
    {
        Player,
        Tutorial,
    }

    private Coroutine intersitialAdCoroutine;

    #region SAVE / RESTORE PROGRESS

    // Sau MỖI action (click spawner / nhả bucket): ghi save ngay xuống disk.
    // OnBeforeSave của ClassicProgressSaveData sẽ tự gọi CaptureTo bên dưới để chụp trạng thái.
    private void HandleInteractionRecorded()
    {
        HAVIGAME.SaveLoad.SaveLoadManager.Save();
    }

    private void ClearSavedProgress()
    {
        ClassicProgressSaveData.RemoveCaptureProvider(this);
        GameData.ClassicProgress.Clear();
        HAVIGAME.SaveLoad.SaveLoadManager.Save();
    }

    /// <summary>Chụp trạng thái ingame hiện tại vào save data (ICaptureProvider).</summary>
    public bool CaptureTo(ClassicProgressSaveData data)
    {
        if (this == null || isWon || isLost || loadedLevelRoot == null || CurrentLevelData == null)
            return false;
        if (gamePlayManager == null || gamePlayManager.HasWon || gamePlayManager.HasLost)
            return false;

        FruitSort.Bucket[] liveBuckets = loadedLevelRoot.GetComponentsInChildren<FruitSort.Bucket>(true);
        FruitSort.ModelDotSpawner[] liveSpawners = loadedLevelRoot.GetComponentsInChildren<FruitSort.ModelDotSpawner>(true);
        FruitSort.ConveyorSpline[] conveyors = loadedLevelRoot.GetComponentsInChildren<FruitSort.ConveyorSpline>(true);

        data.BeginCapture(
            currentLevelNumber,
            FruitSort.LevelData.ComputeConfigHash(CurrentLevelData),
            gamePlayManager.HasMoveLimit ? gamePlayManager.MovesLeft : 0,
            gamePlayManager.HasTimeLimit ? gamePlayManager.TimeLeft : 0f,
            gamePlayManager.score,
            totalBuckets,
            liveSpawners.Length,
            conveyors.Length);

        // Bucket map theo index dựng trong tên "Bucket_{i}_c{color}" (LevelBuilder đặt),
        // vì bucket đầy có thể đã bị worker destroy -> thứ tự hierarchy không còn đủ.
        bool[] captured = new bool[Mathf.Max(0, totalBuckets)];
        List<int> colorBuffer = new List<int>(16);
        for (int i = 0; i < liveBuckets.Length; i++)
        {
            FruitSort.Bucket bucket = liveBuckets[i];
            if (bucket == null) continue;
            int index = ParseBucketBuildIndex(bucket.name, i);
            if (index >= 0 && index < captured.Length) captured[index] = true;

            colorBuffer.Clear();
            bucket.GetContainedColorIds(colorBuffer);
            data.AddBucketState(index, bucket.IsFull, colorBuffer);

            // Dot đang nhả dở (coroutine ReleaseContents) -> lưu như dot bay tại miệng giỏ.
            colorBuffer.Clear();
            bucket.AppendPendingReleaseColors(colorBuffer);
            Vector2 releaseVelocity = bucket.launchDirection.sqrMagnitude > 0.0001f
                ? bucket.launchDirection.normalized * bucket.launchSpeed
                : Vector2.down * bucket.launchSpeed;
            for (int c = 0; c < colorBuffer.Count; c++)
                data.AddFlyingDot(colorBuffer[c], bucket.MouthPosition, releaseVelocity);
        }

        // Bucket đã đầy và bị dọn khỏi scene -> đánh dấu done để restore không bắt chơi lại.
        for (int i = 0; i < captured.Length; i++)
            if (!captured[i]) data.AddBucketState(i, true, null);

        for (int i = 0; i < liveSpawners.Length; i++)
            data.AddSpawnerState(liveSpawners[i] != null ? liveSpawners[i].DotsLeft : 0);

        FruitSort.FallingPixelManager fm = FruitSort.FallingPixelManager.Instance;
        if (fm != null)
        {
            IReadOnlyList<FruitSort.Dot> dots = fm.Dots;
            for (int i = 0; i < dots.Count; i++)
            {
                FruitSort.Dot dot = dots[i];
                if (dot == null || dot.markedForRemoval || dot.capturedByBucket) continue;

                int conveyorIndex = System.Array.IndexOf(conveyors, dot.conveyor);
                bool onBelt = conveyorIndex >= 0 &&
                    (dot.state == FruitSort.DotState.OnBelt || dot.state == FruitSort.DotState.Attracting);

                if (onBelt)
                {
                    data.AddBeltDot(dot.colorId, conveyorIndex, dot.beltProgress, dot.lateralOffset);
                }
                else
                {
                    Vector2 velocity = dot.state == FruitSort.DotState.Launched
                        ? dot.launchVelocity
                        : new Vector2(0f, -Mathf.Max(2f, dot.fallSpeed));
                    data.AddFlyingDot(dot.colorId, dot.transform.position, velocity);
                }
            }
        }

        return true;
    }

    /// <summary>Áp snapshot đã lưu lên level vừa dựng (nếu có và khớp level).</summary>
    private void TryRestoreProgress(int level)
    {
        ClassicProgressSaveData saved = GameData.ClassicProgress;
        if (saved == null || !saved.HasProgress) return;
        if (saved.Level != level) return; // snapshot của level khác -> giữ nguyên, chơi level này từ đầu
        if (loadedLevelRoot == null || gamePlayManager == null) return;

        // LevelData đã bị sửa sau khi chụp (totalDots/spawnCount/moveLimit/timeLimit...)
        // -> snapshot cũ vô nghĩa, bỏ đi để level load đúng theo data mới.
        if (saved.ConfigHash != FruitSort.LevelData.ComputeConfigHash(CurrentLevelData))
        {
            GameData.ClassicProgress.Clear();
            return;
        }

        FruitSort.Bucket[] liveBuckets = loadedLevelRoot.GetComponentsInChildren<FruitSort.Bucket>(true);
        FruitSort.ModelDotSpawner[] liveSpawners = loadedLevelRoot.GetComponentsInChildren<FruitSort.ModelDotSpawner>(true);
        FruitSort.ConveyorSpline[] conveyors = loadedLevelRoot.GetComponentsInChildren<FruitSort.ConveyorSpline>(true);

        // Cấu trúc level phải khớp snapshot (đề phòng level data đổi sau khi update game).
        if (saved.BucketCount != totalBuckets ||
            saved.SpawnerCount != liveSpawners.Length ||
            saved.ConveyorCount != conveyors.Length)
        {
            GameData.ClassicProgress.Clear();
            return;
        }

        // Prefab + scale dot để dựng lại dot trong giỏ và trên băng chuyền.
        FruitSort.Dot dotPrefab = null;
        float dotScale = 0.5f;
        for (int i = 0; i < liveSpawners.Length && dotPrefab == null; i++)
        {
            if (liveSpawners[i] == null || liveSpawners[i].dotPrefab == null) continue;
            dotPrefab = liveSpawners[i].dotPrefab;
            dotScale = liveSpawners[i].dotScale;
        }
        if (dotPrefab == null && CurrentLevelData.spawnerPrefab != null)
            dotPrefab = CurrentLevelData.spawnerPrefab.dotPrefab;

        // ---- Spawner (thứ tự GetComponentsInChildren = thứ tự dựng, spawner không bị destroy) ----
        for (int i = 0; i < liveSpawners.Length; i++)
        {
            if (liveSpawners[i] == null) continue;
            liveSpawners[i].RestoreDotsLeft(saved.Spawners[i].dotsLeft);
        }

        // ---- Bucket (map theo index trong tên) ----
        var bucketByIndex = new Dictionary<int, FruitSort.Bucket>(liveBuckets.Length);
        for (int i = 0; i < liveBuckets.Length; i++)
        {
            if (liveBuckets[i] == null) continue;
            bucketByIndex[ParseBucketBuildIndex(liveBuckets[i].name, i)] = liveBuckets[i];
        }

        for (int i = 0; i < saved.Buckets.Count; i++)
        {
            ClassicProgressSaveData.BucketState state = saved.Buckets[i];
            if (!bucketByIndex.TryGetValue(state.index, out FruitSort.Bucket bucket) || bucket == null)
                continue;

            if (state.done)
            {
                // Bucket đã hoàn thành trước khi save -> tính vào tiến độ win và dọn khỏi scene
                // (không qua OnBucketFull để khỏi kích hoạt hiệu ứng/kiểm tra win giữa chừng).
                filledBuckets++;
                Destroy(bucket.gameObject);
            }
            else
            {
                bucket.RestoreContents(state.colors, dotPrefab, dotScale);
            }
        }

        // ---- Dot trên băng chuyền / đang bay ----
        FruitSort.FallingPixelManager fm = FruitSort.FallingPixelManager.Instance;
        if (fm != null && dotPrefab != null)
        {
            FruitSort.FruitDatabase db = CurrentLevelData.fruitDatabase;
            for (int i = 0; i < saved.Dots.Count; i++)
            {
                ClassicProgressSaveData.DotSnapshot snap = saved.Dots[i];
                FruitSort.Dot dot = Instantiate(dotPrefab);
                FruitSort.FruitData fruit = db != null ? db.GetById(snap.colorId) : null;
                dot.Init(snap.colorId, fruit != null ? fruit.color : Color.white, 1, new Vector2Int(-1, -1));
                dot.transform.localScale = Vector3.one * dotScale;

                if (snap.conveyorIndex >= 0 && snap.conveyorIndex < conveyors.Length)
                {
                    fm.PlaceDotOnBelt(dot, conveyors[snap.conveyorIndex], snap.progress, snap.lateral);
                }
                else
                {
                    Vector2 velocity = snap.velocity.sqrMagnitude > 0.01f ? snap.velocity : Vector2.down * 2f;
                    fm.LaunchDot(dot, snap.position, velocity.normalized, velocity.magnitude, 0f);
                }
            }
        }

        // ---- Moves / time / score ----
        gamePlayManager.RestoreProgress(saved.MovesLeft, saved.TimeLeft, saved.Score);
        if (CurrentLevelData.timeLimit > 0f && generator != null)
        {
            // OnStartLevel đọc generator.Duration để set đồng hồ -> ghi đè bằng giờ còn lại.
            generator.Duration = Mathf.Clamp(
                Mathf.CeilToInt(saved.TimeLeft),
                1,
                Mathf.CeilToInt(CurrentLevelData.timeLimit));
        }

        resumedFromSave = true;
    }

    // Tên bucket do LevelBuilder đặt: "Bucket_{index}_c{colorId}". Lỗi parse -> dùng fallback.
    private static int ParseBucketBuildIndex(string name, int fallback)
    {
        const string prefix = "Bucket_";
        if (string.IsNullOrEmpty(name) || !name.StartsWith(prefix)) return fallback;

        int start = prefix.Length;
        int end = name.IndexOf('_', start);
        string token = end > start ? name.Substring(start, end - start) : name.Substring(start);
        return int.TryParse(token, out int index) ? index : fallback;
    }

    #endregion

    #region FUNC RELATE SHOW ADS


    private void StopShowInterstitialAd()
    {
        if (intersitialAdCoroutine != null) StopCoroutine(intersitialAdCoroutine);
        intersitialAdCoroutine = null;
    }

    private IEnumerator IEShowIntersitialAd(float cappingTime)
    {
        WaitForSeconds waitFoCappingTime = new WaitForSeconds(cappingTime);

        while (true)
        {
            yield return waitFoCappingTime;

            GameAdvertising.TryShowInterstitialAd();
        }
    }

    #endregion
}

#region EVENT ANALYTICS
/*public void LogWinLevelEvent()
{
    GameAnalytics.LogEvent(CreateWinlevelEvent());
}

public void LogLoseLevelEvent(string reason)
{
    GameAnalytics.LogEvent(CreateLoselevelEvent(reason));
}

private GameAnalytics.GameEvent CreateWinlevelEvent()
{
    return GameAnalytics.GameEvent.Create("level_end")
        .Add("level", Option.Level.ToString())
        .Add("result", "win")
        .Add("total_move", TotalMove.ToString())
        .Add("play_time", Mathf.CeilToInt(Time.time - startedTime))
        .Add("use_time_freeze", GetBoosterUsed(ItemID.TimeFreezeBooster))
        .Add("use_reroll", GetBoosterUsed(ItemID.RerollBooster))
        .Add("use_more_time", GetBoosterUsed(ItemID.MoreTimeBooster))
        .Add("use_magic_wand", GetBoosterUsed(ItemID.MagicWandBooster))
        .Add("use_double_star", GetBoosterUsed(ItemID.DoubleStarBooster))
        .Add("use_big_hammer", GetBoosterUsed(ItemID.BigHammerBooster))
        .Add("use_small_hammer", GetBoosterUsed(ItemID.SmallHammerBooster))
        .Add("use_used", boosterUsed.ToString());
}

private GameAnalytics.GameEvent CreateLoselevelEvent(string reason)
{
    return GameAnalytics.GameEvent.Create("level_end")
        .Add("level", Option.Level.ToString())
        .Add("result", "fail")
        .Add("reason", reason)
        .Add("play_time", Mathf.CeilToInt(Time.time - startedTime).ToString())
        .Add("use_time_freeze", GetBoosterUsed(ItemID.TimeFreezeBooster))
        .Add("use_reroll", GetBoosterUsed(ItemID.RerollBooster))
        .Add("use_more_time", GetBoosterUsed(ItemID.MoreTimeBooster))
        .Add("use_magic_wand", GetBoosterUsed(ItemID.MagicWandBooster))
        .Add("use_double_star", GetBoosterUsed(ItemID.DoubleStarBooster))
        .Add("use_big_hammer", GetBoosterUsed(ItemID.BigHammerBooster))
        .Add("use_small_hammer", GetBoosterUsed(ItemID.SmallHammerBooster))
        .Add("use_used", boosterUsed.ToString());
}*/
#endregion
