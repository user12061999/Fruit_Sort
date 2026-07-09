using System;
using HAVIGAME;
using HAVIGAME.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FruitSort;

public class GamePanel : UIFrame
{
    [Header("[References]")]
    [SerializeField] private TextMeshProUGUI txtLevel;
    [SerializeField] private Button btnPause;
    [SerializeField] private TextMeshProUGUI txtCountDownTime;
    [SerializeField] private TextMeshProUGUI txtMovesLeft;
    [SerializeField] private Image circle,rectangle,pause;
    [SerializeField] private Sprite[] circleSprites, rectangleSprites,pauseSprites;

    [Header("[Time Freeze Booster]")]
    [Tooltip("Nút dùng booster đóng băng giờ. Để trống nếu chưa có UI.")]
    [SerializeField] private Button btnFreezeTime;
    [Tooltip("Text hiển thị số booster đang có (con của nút).")]
    [SerializeField] private TextMeshProUGUI txtFreezeCount;
    [Tooltip("Số giây đóng băng mỗi lần dùng.")]
    [SerializeField] private float freezeDuration = 10f;

    private static readonly Color FreezeClockColor = new Color(0.45f, 0.85f, 1f);

    private void Start()
    {
        btnPause.onClick.AddListener(PauseGame);
        if (btnFreezeTime != null) btnFreezeTime.onClick.AddListener(OnClickFreezeTime);
        EventDispatcher.AddListener<GameEvent.LevelTimeChanged>(UpdateCountdownTime);
        EventDispatcher.AddListener<GameEvent.LevelMovesChanged>(UpdateMovesLeft);
        EventDispatcher.AddListener<GameEvent.LevelCountdownChanged>(LevelCountdownChanged);
        EventDispatcher.AddListener<GameEvent.PlayerInventoryChanged>(OnInventoryChanged);
    }
    private void OnDestroy()
    {
        EventDispatcher.RemoveListener<GameEvent.LevelTimeChanged>(UpdateCountdownTime);
        EventDispatcher.RemoveListener<GameEvent.LevelMovesChanged>(UpdateMovesLeft);
        EventDispatcher.RemoveListener<GameEvent.LevelCountdownChanged>(LevelCountdownChanged);
        EventDispatcher.RemoveListener<GameEvent.PlayerInventoryChanged>(OnInventoryChanged);
    }
    private void LevelCountdownChanged(GameEvent.LevelCountdownChanged args)
    {
        // Đồng hồ chuyển màu băng khi bị đóng băng; nút freeze khoá trong lúc đang freeze.
        if (txtCountDownTime != null)
            txtCountDownTime.color = args.IsPaused ? FreezeClockColor : Color.white;
        if (btnFreezeTime != null)
            btnFreezeTime.interactable = !args.IsPaused;
    }

    private void OnInventoryChanged(GameEvent.PlayerInventoryChanged args)
    {
        if (args.ItemStackChange.Id == ItemID.TimeFreezeBooster) RefreshFreezeCount();
    }

    private void OnClickFreezeTime()
    {
        ClassicLevelController controller = ClassicLevelController.instance;
        if (controller == null || !controller.CanFreezeTime) return;

        ItemStack cost = new ItemStack(ItemID.TimeFreezeBooster, 1);
        if (GameData.Inventory.IsEnought(cost))
        {
            if (controller.FreezeCountdown(freezeDuration))
            {
                GameData.Inventory.Remove(cost, "booster");
                RefreshFreezeCount();
            }
        }
        else
        {
            // Hết booster -> xem ad thưởng để dùng 1 lần.
            GameAdvertising.TryShowRewardedAd(() =>
            {
                ClassicLevelController c = ClassicLevelController.instance;
                if (c != null) c.FreezeCountdown(freezeDuration);
            });
        }
    }

    private void RefreshFreezeCount()
    {
        if (txtFreezeCount == null) return;
        int count = GameData.Inventory.GetCount(ItemID.TimeFreezeBooster);
        // Hết booster -> hiện icon ad thay số (dùng qua rewarded ad).
        txtFreezeCount.text = count > 0 ? count.ToString() : "AD";
    }
    protected override void OnShow(bool instant = false)
    {
        base.OnShow(instant);
        EnsureMovesText();
        txtLevel.text = string.Format("LEVEL{0}", GameController.Instance.LoadLevelOption.Level);
        int currentLevel = GameData.Classic.LevelUnlocked;
        string paths = $"LevelSO/Level_{currentLevel}";
        GamePlayManager gamePlayManager = GamePlayManager.Instance;
        if (gamePlayManager != null)
        {
            SetMovesText(gamePlayManager.HasMoveLimit, gamePlayManager.MovesLeft);
        }
        RefreshFreezeCount();
        // Level không giới hạn giờ -> ẩn nút freeze.
        if (btnFreezeTime != null)
        {
            ClassicLevelController controller = ClassicLevelController.instance;
            bool hasTimeLimit = controller != null &&
                controller.CurrentLevelData != null &&
                controller.CurrentLevelData.timeLimit > 0f;
            btnFreezeTime.gameObject.SetActive(hasTimeLimit);
            btnFreezeTime.interactable = true;
        }
    }

    protected override void OnBack()
    {
        PauseGame();
    }

    // UIManager gọi Pause/Resume cho frame ngay dưới top mỗi khi push/đóng popup
    // -> mọi popup (PausePanel, WinPanel, LosePanel, Dialog...) đều tự đóng băng ingame.
    protected override void OnPause()
    {
        base.OnPause();
        if (ClassicLevelController.instance != null)
        {
            ClassicLevelController.instance.PauseGameplay();
        }
    }

    protected override void OnResume()
    {
        base.OnResume();
        if (ClassicLevelController.instance != null)
        {
            ClassicLevelController.instance.ResumeGameplay();
        }
    }

    private void PauseGame()
    {
        UIManager.Instance.Push<PausePanel>();
    }
    public void SetCountdownTime(int seconds)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
        if (timeSpan.TotalHours > 1)
        {
            txtCountDownTime.text = timeSpan.ToString(@"hh\:mm\:ss");
        }
        else
        {
            txtCountDownTime.text = timeSpan.ToString(@"mm\:ss");
        }
    }
    public void UpdateCountdownTime(GameEvent.LevelTimeChanged args)
    {
        SetCountdownTime(args.RemainingSeconds);
        if (args.RemainingSeconds == 0)
        {
            //ConfigDatabase.Instance.AudioConfig.lastTickSound.Play();
            //warringVfx.SetActive(false);
        }
        else if (args.RemainingSeconds <= 10)
        {
            //txtCountDownTime.color = Color.red;
            //ConfigDatabase.Instance.AudioConfig.tickSound.Play();
            //warringVfx.SetActive(true);
        }
        else
        {
            txtCountDownTime.color = Color.white;
            //warringVfx.SetActive(false);
        }
    }

    public void UpdateMovesLeft(GameEvent.LevelMovesChanged args)
    {
        SetMovesText(args.HasMoveLimit, args.RemainingMoves);
    }

    private void SetMovesText(bool hasMoveLimit, int remainingMoves)
    {
        EnsureMovesText();
        if (txtMovesLeft == null)
        {
            return;
        }

        txtMovesLeft.text = hasMoveLimit ? $"Moves: {remainingMoves}" : "Moves: --";
    }

    private void EnsureMovesText()
    {
        if (txtMovesLeft != null || txtCountDownTime == null)
        {
            return;
        }

        txtMovesLeft = Instantiate(txtCountDownTime, txtCountDownTime.transform.parent);
        txtMovesLeft.name = "txtMovesLeft_Auto";
        txtMovesLeft.fontSize = Mathf.Max(28f, txtCountDownTime.fontSize * 0.7f);
        txtMovesLeft.alignment = TextAlignmentOptions.Center;
        txtMovesLeft.rectTransform.anchoredPosition =
            txtCountDownTime.rectTransform.anchoredPosition + new Vector2(0f, -52f);
    }
}
