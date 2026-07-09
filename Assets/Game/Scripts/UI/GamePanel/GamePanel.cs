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
    private void Start()
    {
        btnPause.onClick.AddListener(PauseGame);
        EventDispatcher.AddListener<GameEvent.LevelTimeChanged>(UpdateCountdownTime);
        EventDispatcher.AddListener<GameEvent.LevelMovesChanged>(UpdateMovesLeft);
        EventDispatcher.AddListener<GameEvent.LevelCountdownChanged>(LevelCountdownChanged);
    }
    private void OnDestroy()
    {
        EventDispatcher.RemoveListener<GameEvent.LevelTimeChanged>(UpdateCountdownTime);
        EventDispatcher.RemoveListener<GameEvent.LevelMovesChanged>(UpdateMovesLeft);
        EventDispatcher.RemoveListener<GameEvent.LevelCountdownChanged>(LevelCountdownChanged);
    }
    private void LevelCountdownChanged(GameEvent.LevelCountdownChanged args)
    {
        /*if (args.IsPaused) {
            spinTime.gameObject.SetActive(true);
            spinTime.SetAnim(0, "frezee", null);
            MoveVFX();
        } else {

            spinTime.SetAnim(0, "frezee2", () => {
                spinTime.gameObject.SetActive(false);
            });

        }
        freezeVfx.SetActive(args.IsPaused);
        */

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
    }

    protected override void OnBack()
    {
        PauseGame();
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
