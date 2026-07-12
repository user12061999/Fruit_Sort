using System;
using DG.Tweening;
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

    [Header("[Magnet Booster]")]
    [Tooltip("Nút dùng booster nam châm (hút dot đúng màu về giỏ sắp đầy nhất). Để trống nếu chưa có UI.")]
    [SerializeField] private Button btnMagnet;
    [Tooltip("Text hiển thị số booster nam châm đang có (con của nút).")]
    [SerializeField] private TextMeshProUGUI txtMagnetCount;

    [Header("[Combo & Cảnh báo dot]")]
    [Tooltip("Text hiện 'COMBO xN'. Để trống = tự tạo (clone style đồng hồ).")]
    [SerializeField] private TextMeshProUGUI txtCombo;
    [Tooltip("Text cảnh báo sắp thiếu dot. Để trống = tự tạo (clone style đồng hồ).")]
    [SerializeField] private TextMeshProUGUI txtSupplyWarning;

    private static readonly Color FreezeClockColor = new Color(0.45f, 0.85f, 1f);
    private const string ComboFadeTweenId = "GamePanel_ComboFade";
    private const string SupplyBlinkTweenId = "GamePanel_SupplyBlink";

    private void Start()
    {
        btnPause.onClick.AddListener(PauseGame);
        if (btnFreezeTime != null) btnFreezeTime.onClick.AddListener(OnClickFreezeTime);
        if (btnMagnet != null) btnMagnet.onClick.AddListener(OnClickMagnet);
        EventDispatcher.AddListener<GameEvent.LevelTimeChanged>(UpdateCountdownTime);
        EventDispatcher.AddListener<GameEvent.LevelMovesChanged>(UpdateMovesLeft);
        EventDispatcher.AddListener<GameEvent.LevelCountdownChanged>(LevelCountdownChanged);
        EventDispatcher.AddListener<GameEvent.PlayerInventoryChanged>(OnInventoryChanged);
        GamePlayManager.onComboChanged += OnComboChanged;
        GamePlayManager.onSupplyRiskChanged += OnSupplyRiskChanged;
    }
    private void OnDestroy()
    {
        EventDispatcher.RemoveListener<GameEvent.LevelTimeChanged>(UpdateCountdownTime);
        EventDispatcher.RemoveListener<GameEvent.LevelMovesChanged>(UpdateMovesLeft);
        EventDispatcher.RemoveListener<GameEvent.LevelCountdownChanged>(LevelCountdownChanged);
        EventDispatcher.RemoveListener<GameEvent.PlayerInventoryChanged>(OnInventoryChanged);
        GamePlayManager.onComboChanged -= OnComboChanged;
        GamePlayManager.onSupplyRiskChanged -= OnSupplyRiskChanged;
        DOTween.Kill(ComboFadeTweenId);
        DOTween.Kill(SupplyBlinkTweenId);
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
        if (args.ItemStackChange.Id == ItemID.MagnetBooster) RefreshMagnetCount();
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

    private void OnClickMagnet()
    {
        ClassicLevelController controller = ClassicLevelController.instance;
        if (controller == null || !controller.CanUseMagnet) return;

        ItemStack cost = new ItemStack(ItemID.MagnetBooster, 1);
        if (GameData.Inventory.IsEnought(cost))
        {
            // UseMagnetBooster trả false khi không có dot nào để hút -> KHÔNG trừ item.
            if (controller.UseMagnetBooster())
            {
                GameData.Inventory.Remove(cost, "booster");
                RefreshMagnetCount();
            }
        }
        else
        {
            // Hết booster -> xem ad thưởng để dùng 1 lần.
            GameAdvertising.TryShowRewardedAd(() =>
            {
                ClassicLevelController c = ClassicLevelController.instance;
                if (c != null) c.UseMagnetBooster();
            });
        }
    }

    private void RefreshMagnetCount()
    {
        if (txtMagnetCount == null) return;
        int count = GameData.Inventory.GetCount(ItemID.MagnetBooster);
        txtMagnetCount.text = count > 0 ? count.ToString() : "AD";
    }

    // ================= COMBO + CẢNH BÁO THIẾU DOT =================

    private void OnComboChanged(int combo)
    {
        EnsureComboText();
        if (txtCombo == null) return;

        float window = GamePlayManager.Instance != null ? GamePlayManager.Instance.comboWindow : 2f;

        txtCombo.gameObject.SetActive(true);
        txtCombo.text = $"COMBO x{combo}";
        txtCombo.alpha = 1f;

        txtCombo.transform.DOKill(true);
        txtCombo.transform.localScale = Vector3.one;
        txtCombo.transform.DOPunchScale(Vector3.one * 0.35f, 0.25f, 6, 0.7f);

        // Hết cửa sổ combo thì fade đi; sort tiếp thì tween này bị kill và đặt lại.
        DOTween.Kill(ComboFadeTweenId);
        DOTween.To(() => txtCombo.alpha, a => txtCombo.alpha = a, 0f, 0.35f)
               .SetDelay(window)
               .SetId(ComboFadeTweenId)
               .OnComplete(() => txtCombo.gameObject.SetActive(false));
    }

    private void OnSupplyRiskChanged(GamePlayManager.DotSupplyRisk risk)
    {
        EnsureSupplyWarningText();
        if (txtSupplyWarning == null) return;

        DOTween.Kill(SupplyBlinkTweenId);

        bool show = risk != GamePlayManager.DotSupplyRisk.None;
        txtSupplyWarning.gameObject.SetActive(show);
        if (!show) return;

        bool starved = risk == GamePlayManager.DotSupplyRisk.Starved;
        txtSupplyWarning.text = starved
            ? "Not enough dots for a bucket!"
            : "No spare dots - sort carefully!";
        txtSupplyWarning.color = starved ? new Color(1f, 0.3f, 0.25f) : new Color(1f, 0.7f, 0.2f);
        txtSupplyWarning.alpha = 1f;

        DOTween.To(() => txtSupplyWarning.alpha, a => txtSupplyWarning.alpha = a, 0.35f, 0.5f)
               .SetLoops(-1, LoopType.Yoyo)
               .SetEase(Ease.InOutSine)
               .SetId(SupplyBlinkTweenId);
    }

    private void EnsureComboText()
    {
        if (txtCombo != null || txtCountDownTime == null) return;

        txtCombo = Instantiate(txtCountDownTime, txtCountDownTime.transform.parent);
        txtCombo.name = "txtCombo_Auto";
        txtCombo.fontSize = Mathf.Max(34f, txtCountDownTime.fontSize * 0.9f);
        txtCombo.fontStyle = FontStyles.Bold;
        txtCombo.color = new Color(1f, 0.85f, 0.2f);
        txtCombo.alignment = TextAlignmentOptions.Center;
        txtCombo.rectTransform.anchoredPosition =
            txtCountDownTime.rectTransform.anchoredPosition + new Vector2(0f, -104f);
        txtCombo.gameObject.SetActive(false);
    }

    private void EnsureSupplyWarningText()
    {
        if (txtSupplyWarning != null || txtCountDownTime == null) return;

        txtSupplyWarning = Instantiate(txtCountDownTime, txtCountDownTime.transform.parent);
        txtSupplyWarning.name = "txtSupplyWarning_Auto";
        txtSupplyWarning.fontSize = Mathf.Max(26f, txtCountDownTime.fontSize * 0.55f);
        txtSupplyWarning.fontStyle = FontStyles.Bold;
        txtSupplyWarning.alignment = TextAlignmentOptions.Center;
        txtSupplyWarning.rectTransform.anchoredPosition =
            txtCountDownTime.rectTransform.anchoredPosition + new Vector2(0f, -150f);
        txtSupplyWarning.gameObject.SetActive(false);
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
        RefreshMagnetCount();
        // Ẩn text combo/cảnh báo còn sót từ ván trước.
        DOTween.Kill(ComboFadeTweenId);
        DOTween.Kill(SupplyBlinkTweenId);
        if (txtCombo != null) txtCombo.gameObject.SetActive(false);
        if (txtSupplyWarning != null) txtSupplyWarning.gameObject.SetActive(false);
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
