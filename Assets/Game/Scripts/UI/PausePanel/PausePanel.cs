using HAVIGAME.Scenes;
using HAVIGAME.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PausePanel : UIFrame
{
    [Header("[References]")]
    [SerializeField] private Button btnHome;
    [SerializeField] private Button btnReplay;
    [SerializeField] private Button btnOkReplay;
    [SerializeField] private Button btnBackToSetting;
    [SerializeField] private Button btnOkHome;

    [SerializeField] private GameObject panelLoseHeart;
    [SerializeField] private TextMeshProUGUI textLevel;
    int index = 0; // 0: Home, 1: Retry

    private void Start()
    {
        btnHome.onClick.AddListener(BtnHome);
        btnReplay.onClick.AddListener(BtnReplay);
        btnOkReplay.onClick.AddListener(SlectRelayOrBackToHome);
        btnOkHome.onClick.AddListener(SlectRelayOrBackToHome);
        btnBackToSetting.onClick.AddListener(ButtonBackToSetting);
    }

    protected override void OnShow(bool instant = false)
    {
        base.OnShow(instant);
        panelLoseHeart.SetActive(false);
        SetTextLevel();
    }

    void SetTextLevel()
    {
        int level = GameController.Instance.LoadLevelOption.Level;
        textLevel.text = string.Format("Level {0}", level);
    }
    private void BtnHome()
    {
        index = 0;
        btnOkReplay.gameObject.SetActive(false);
        btnOkHome.gameObject.SetActive(true);
        panelLoseHeart.SetActive(true);
    }

    private void SkipGame()
    {
        GameController.Instance.SkipGame();
        GameController.Instance.DestroyGame();

        int levelToPlay = GameData.Classic.LevelUnlocked;
        GameSceneController.pendingLoadLevelOption = LoadLevelOption.Create(levelToPlay);
        ScenesManager.Instance.LoadSceneAsyn(GameScene.ByIndex.Game);
    }
    private void ButtonBackToSetting()
    {
        panelLoseHeart.SetActive(false);
    }

    private void SlectRelayOrBackToHome()
    {
        bool isEnought = GameData.Inventory.IsEnought(new ItemStack(ItemID.Heart, 1));
        if (!isEnought)
        {

            if (UIManager.HasInstance)
            {
                string title;
                string message;
                if (index == 0)
                {
                    GameController.Instance.DestroyGame();
                    ScenesManager.Instance.LoadSceneAsyn(GameScene.ByIndex.Home);
                }
                else
                {
                    //RETRY
                    title = "Retry!";
                    message = "You don't have enough hearts to retry!";
                    UIManager.Instance.Push<DialogPanel>().Dialog(title, message);
                }
            }
        }
        else
        {
            if (index == 0)
            {
                //HOME
                GameController.Instance.DestroyGame();
                ScenesManager.Instance.LoadSceneAsyn(GameScene.ByIndex.Home);
            }
            else
            {
                //RETRY
                GameController.Instance.DestroyGame();
                int levelToPlay = GameData.Classic.LevelUnlocked;
                GameSceneController.pendingLoadLevelOption = LoadLevelOption.Create(levelToPlay);
                ScenesManager.Instance.LoadSceneAsyn(GameScene.ByIndex.Game);
            }
        }
    }
    private void BtnReplay()
    {
        index = 1; // Set index to 1 for Retry
        btnOkReplay.gameObject.SetActive(true);
        btnOkHome.gameObject.SetActive(false);
        panelLoseHeart.SetActive(true);
    }
}
