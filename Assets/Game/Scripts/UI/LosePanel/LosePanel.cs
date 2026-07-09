using DG.Tweening;
using HAVIGAME;
using HAVIGAME.Scenes;
using HAVIGAME.UI;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LosePanel : GameUIFrame
{
    [Header("[References]")]
    [SerializeField] private Button btnClose;
    [SerializeField] private ItemView rewardViewPrefab;
    [SerializeField] private Transform rewardViewContainer;
    [SerializeField] private ItemView priceView; [SerializeField] private ItemStack priceItem;
    [SerializeField] private Button btnReviveAds, btnReviveGold;
    [Header("[Revive]")]
    [Tooltip("Số giây cộng thêm khi revive sau thua do hết giờ.")]
    [SerializeField] private int reviveSeconds = 20;
    [Tooltip("Số lượt move cộng thêm khi revive sau thua do hết move.")]
    [SerializeField] private int reviveMoves = 5;
    [Header("[Lose Reason UI]")]
    [Tooltip("Object UI hiển thị khi thua do hết giờ (lý do thua + phần thưởng revive).")]
    [SerializeField] private GameObject loseByTimeUI;
    [Tooltip("Object UI hiển thị khi thua do hết lượt move (lý do thua + phần thưởng revive).")]
    [SerializeField] private GameObject loseByMovesUI;

    private int itemId;
    private Action<bool> callback;
    private Tween audioTween;
    private CollectionView<ItemView, ItemStack> views;

    private void Awake()
    {
        views = new CollectionView<ItemView, ItemStack>(rewardViewPrefab, rewardViewContainer);
    }
    private void Start()
    {
        btnReviveAds.onClick.AddListener(OnClickButtonAds);
        btnReviveGold.onClick.AddListener(OnClickButtonGold);
        btnClose.onClick.AddListener(OnClickButtonHome);
    }

    protected override void OnShow(bool instant = false)
    {
        base.OnShow(instant);
        UpdateLoseReasonUI();

        //GameAnalytics.LogEvent(GameAnalytics.GameEvent.Create("iap_show").Add("position", "ingame"));
    }

    /// <summary>Thua do hết move? (mặc định false = thua do hết giờ, kể cả khi thua qua LevelTimer).</summary>
    private bool IsLostByMoves()
    {
        FruitSort.GamePlayManager gamePlayManager = FruitSort.GamePlayManager.Instance;
        return gamePlayManager != null &&
            gamePlayManager.LastLoseReason == FruitSort.GamePlayManager.LoseReason.OutOfMoves;
    }

    private void UpdateLoseReasonUI()
    {
        bool byMoves = IsLostByMoves();
        if (loseByTimeUI != null) loseByTimeUI.SetActive(!byMoves);
        if (loseByMovesUI != null) loseByMovesUI.SetActive(byMoves);
    }
    protected override void OnShowCompleted()
    {
        base.OnShowCompleted();
        ShowPack();
    }
    public void CheckButton()
    {
        btnReviveAds.gameObject.SetActive(false);
        btnReviveGold.gameObject.SetActive(false);

        //print(GameData.Inventory.Get(priceItem));
        bool isEnought = GameData.Inventory.IsEnought(priceItem);
        //GameData.Inventory.Add(new ItemStack(ItemID.Coin, 100), "trade");
        // GameData.Inventory.Remove(new ItemStack(ItemID.Coin, 100), "trade");
        if (isEnought)
        {
            btnReviveGold.gameObject.SetActive(true);
        }
        else
        {
            btnReviveAds.gameObject.SetActive(true);
        }
    }

    private void ShowPack()
    {

    }

    protected override void OnHide(bool instant = false)
    {
        base.OnHide(instant);

        audioTween?.Kill();
    }

    protected override void OnBack()
    {

    }

    public void SetLostRewards(ItemStack[] rewards)
    {
        views.SetModels(rewards).Show();
    }
    public void SetCoin(ItemStack itemStack)
    {
        priceItem = itemStack;
        priceView.SetModel(itemStack).Show();
    }
    // public void SetItem(int itemId, Action<bool> callback)
    // {
    //     this.itemId = itemId;
    //     this.callback = callback;

    //     ItemData data = ItemDatabase.Instance.GetDataById(itemId);

    //     priceView.SetModel(data.Price).Show();
    // }

    private void OnPurchase()
    {
        // ItemData data = ItemDatabase.Instance.GetDataById(itemId);

        bool isEnought = GameData.Inventory.IsEnought(priceItem);

        if (isEnought)
        {
            GameData.Inventory.Remove(priceItem, "trade");
            GameData.Inventory.Add(new ItemStack(itemId, 1), "trade");
            Hide();
            callback?.Invoke(true);
        }
        else
        {
            ItemData priceData = ItemDatabase.Instance.GetDataById(priceItem.Id);
            NotifyPopupManager.Instance.PushNotify(Utility.Text.Format("You don't have enough {0}.", priceData.Name));
        }
    }

    public void OnRevive()
    {
        ClassicLevelController controller = ClassicLevelController.instance;
        if (controller == null) return;

        // Revive theo đúng lý do thua: hết move -> cộng move, còn lại -> cộng giờ.
        if (IsLostByMoves())
        {
            controller.AddMoreMoves(reviveMoves);
        }
        else
        {
            controller.AddMoreSeconds(reviveSeconds);
        }
    }
    private void OnAdsPurchase()
    {
        //GameData.Inventory.Add(new ItemStack(itemId, 1), "rewarded_ad");
        Hide();
        callback?.Invoke(true);
    }

    public void OnClickButtonGold()
    {
        //ItemData data = ItemDatabase.Instance.GetDataById(itemId);
        bool isEnought = GameData.Inventory.IsEnought(priceItem);
        if (isEnought)
        {
            GameData.Inventory.Remove(priceItem);
            OnRevive();
            Hide();
            callback?.Invoke(true);
        }
    }
    public void OnClickButtonAds()
    {
        GameAdvertising.TryShowRewardedAd(() =>
        {
            OnRevive();
            Hide();
            callback?.Invoke(true);
        });
    }
    public void OnClickButtonHome()
    {
        GameController.Instance.DestroyGame();
        ScenesManager.Instance.LoadSceneAsyn(GameScene.ByIndex.Home);
    }

    private void OnCloseButtonClicked()
    {
        Hide();
        callback?.Invoke(false);
    }
}
