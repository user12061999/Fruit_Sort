using System;
using HAVIGAME;
using HAVIGAME.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GamePanel : UIFrame
{
    [Header("[References]")]
    [SerializeField] private TextMeshProUGUI txtLevel;
    [SerializeField] private Button btnPause;
    [SerializeField] private TextMeshProUGUI txtCountDownTime;
    [SerializeField] private Image circle,rectangle,pause;
    [SerializeField] private Sprite[] circleSprites, rectangleSprites,pauseSprites;
    private void Start()
    {
        btnPause.onClick.AddListener(PauseGame);
        EventDispatcher.AddListener<GameEvent.LevelTimeChanged>(UpdateCountdownTime);
        EventDispatcher.AddListener<GameEvent.LevelCountdownChanged>(LevelCountdownChanged);
    }
    private void OnDestroy()
    {
        EventDispatcher.RemoveListener<GameEvent.LevelTimeChanged>(UpdateCountdownTime);
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
        txtLevel.text = string.Format("LEVEL{0}", GameController.Instance.LoadLevelOption.Level);
        int currentLevel = GameData.Classic.LevelUnlocked;
        string paths = $"LevelSO/Level_{currentLevel}";
        
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
}
