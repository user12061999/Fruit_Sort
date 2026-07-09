using DG.Tweening;
using HAVIGAME.Scenes;
using HAVIGAME.UI;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class WinPanel : UIFrame
{
    [Header("[References]")]
    [SerializeField]
    private Button btnClaim, btnClaimAds, btnHome;

    [SerializeField] private ItemView rewardView;
    private ItemStack priceItem;

    [SerializeField] private ItemView rewardWithAdsView;

    [SerializeField] private ItemView rewardViewPrefab;
    [SerializeField] private Transform rewardViewContainer;

    [Header("[Stars]")]
    [Tooltip("3 icon sao theo thứ tự trái->phải; bật theo số sao đạt được. Để trống nếu prefab chưa có UI sao.")]
    [SerializeField] private GameObject[] starIcons;
    [Tooltip("Giãn cách giữa các sao khi hiện lần lượt (giây). 0 = hiện cùng lúc.")]
    [SerializeField] private float starRevealInterval = 0.25f;

    private Sequence sequence;
    private Sequence sequences;

    private ItemStack starReward;
    private Tween audioTween;
    private CollectionView<ItemView, ItemStack> views;

    private void Awake()
    {
        views = new CollectionView<ItemView, ItemStack>(rewardViewPrefab, rewardViewContainer);
    }

    private void Start()
    {
        btnClaim.onClick.AddListener(OnButtonClaimClicked);
        btnClaimAds.onClick.AddListener(OnButtonClaimAdsClicked);
        btnHome.onClick.AddListener(OnClickButtonHome);
    }

    protected override void OnShow(bool instant = false)
    {
        base.OnShow(instant);
        audioTween?.Kill();
        sequence?.Kill();
        sequence = null;
        sequences = null;
        ShowClaimButton();
    }

    protected override void OnHide(bool instant = false)
    {
        base.OnHide(instant);
        sequence?.Kill();
        sequence = null;
        audioTween?.Kill();
    }

    protected override void OnBack()
    {
    }

    public void SetRewards(ItemStack[] rewards)
    {
        views.SetModels(rewards).Show();
    }

    /// <summary>Hiện số sao đạt được (1-3), từng sao pop lần lượt.</summary>
    public void SetStars(int stars)
    {
        if (starIcons == null || starIcons.Length == 0) return;

        for (int i = 0; i < starIcons.Length; i++)
        {
            GameObject icon = starIcons[i];
            if (icon == null) continue;

            bool earned = i < stars;
            if (!earned)
            {
                icon.SetActive(false);
                continue;
            }

            icon.SetActive(true);
            icon.transform.DOKill();
            icon.transform.localScale = Vector3.zero;
            // SetUpdate(true): WinPanel hiện lên là ingame pause (timeScale = 0),
            // tween phải chạy unscaled nếu không sao sẽ đứng im ở scale 0.
            icon.transform.DOScale(1f, 0.3f)
                .SetEase(Ease.OutBack)
                .SetDelay(0.3f + i * Mathf.Max(0f, starRevealInterval))
                .SetUpdate(true);
        }
    }
    void ShowClaimButton()
    {
        btnClaim.gameObject.SetActive(false);
        btnClaimAds.gameObject.SetActive(true);
        sequences?.Kill();

        // Create new sequence for delayed button activation
        // SetUpdate(true): WinPanel hiện lên là ingame pause (timeScale = 0) —
        // không có nó thì interval 3s đứng im và btnClaim không bao giờ hiện.
        sequences = DOTween.Sequence()
            .AppendInterval(3f)  // Wait for 3 seconds
            .AppendCallback(() =>
            {
                btnClaim.gameObject.SetActive(true);
            })
            .SetUpdate(true);
    }

    private void OnButtonClaimClicked()
    {
        GameData.Inventory.Add(new ItemStack(ItemID.Coin, ConfigDatabase.Instance.CoinWin));
        GameAdvertising.TryShowInterstitialAd();
        if (GameController.Instance.DestroyGame())
        {
            ClassicLevelController levelController =
                GameController.Instance.LevelController as ClassicLevelController;
            GameSceneController.pendingLoadLevelOption = LoadLevelOption.Create(GameData.Classic.LevelUnlocked);

            ScenesManager.Instance.LoadSceneAsyn(GameScene.ByIndex.Game);
        }
    }
    public void SetCoin(ItemStack itemStack)
    {
        priceItem = itemStack;
        priceItem.Stack(ConfigDatabase.Instance.CoinWin);
        rewardView.SetModel(itemStack).Show();
        rewardWithAdsView.SetModel(priceItem).Show();
    }
    private void OnButtonClaimAdsClicked()
    {
        // GameData.Inventory.Add(new ItemStack(ItemID.Coin, ConfigDatabase.Instance.CoinWin * 2));
        GameAdvertising.TryShowRewardedAd(() =>
        {
            GameData.Inventory.Add(new ItemStack(ItemID.Coin, ConfigDatabase.Instance.CoinWin * 2));
            ClassicLevelController levelController =
                GameController.Instance.LevelController as ClassicLevelController;
            GameSceneController.pendingLoadLevelOption = LoadLevelOption.Create(GameData.Classic.LevelUnlocked);

            ScenesManager.Instance.LoadSceneAsyn(GameScene.ByIndex.Game);
        }, () => { });
    }

    public void OnClickButtonHome()
    {
        GameController.Instance.DestroyGame();
        ScenesManager.Instance.LoadSceneAsyn(GameScene.ByIndex.Home);
    }
}