using DG.Tweening;
using HAVIGAME;
using HAVIGAME.Scenes;
using HAVIGAME.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassicLevelController : LevelController
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
    }

    private void OnDestroy()
    {
        EventDispatcher.RemoveListener<GameEvent.DestroyBlockShape>(OnBlockShapeChange);
        FruitSort.Bucket.OnBucketFull -= HandleBucketFull;

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
        dictBooster = new Dictionary<int, int>();

        LoadLevelData(level);

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
        totalBuckets = loadedLevelRoot.GetComponentsInChildren<FruitSort.Bucket>(true).Length;
        filledBuckets = 0;

        UpdateCameraToLoadedLevel();

        if (generator != null && data.timeLimit > 0f)
        {
            generator.Duration = Mathf.CeilToInt(data.timeLimit);
        }

        return true;
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
        GameData.Inventory.Remove(new ItemStack(ItemID.Heart, 1));
        gamePanel = UIManager.Instance.Push<GamePanel>();
        gamePanel.SetCountdownTime(generator.Duration);
        gamePanel.Interactable = true;
        countBuyTime = 0;
        GameAdvertising.TryShowBannerAd(GameAdvertising.GameAdPosition.BottomCenter);
        int seconds = generator.Duration;


        StartCountdown(seconds);
        PauseCountdown(false);
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
        if (isWon) return;
        timer.Pause();
        gamePanel.Interactable = false;
        CheckHideTutorial();
        StopShowInterstitialAd();
        Time.timeScale = 1;

        countBuyTime++;

        DOVirtual.DelayedCall(1.5f, () =>
        {
            if (isWon) return;
            gamePanel.Interactable = true;
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

        timer.Stop();

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
        timer.AddTime(amount);
        timer.Resume();
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
