using HAVIGAME.UI;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanel : UITab
{

    [Header("[References]")]
    [SerializeField] private Button btnAdsHeart;
    [SerializeField] private Button btnAdsCoin;
    void Start()
    {
        btnAdsHeart.onClick.AddListener(AdsHeart);
        btnAdsCoin.onClick.AddListener(AdsCoin);
    }

    private void AdsHeart()
    {
        // Implement logic to buy heart
        Debug.Log("Buy Heart");
        GameAdvertising.TryShowRewardedAd(() =>
                {
                    // Callback when ad is successfully shown
                    Debug.Log("Rewarded ad shown successfully.");
                    // Add logic to reward the player with hearts here
                    GameData.Inventory.Add(new ItemStack(ItemID.Heart, 1), "rewarded_ad");
                }, () =>
                {
                    // Callback when ad fails to show
                    Debug.Log("Failed to show rewarded ad.");
                });

    }
    private void AdsCoin()
    {
        // Implement logic to buy coin
        Debug.Log("Buy Coin");

        GameAdvertising.TryShowRewardedAd(() =>
                       {
                           // Callback when ad is successfully shown
                           Debug.Log("Rewarded ad shown successfully.");
                           // Add logic to reward the player with hearts here
                           GameData.Inventory.Add(new ItemStack(ItemID.Coin, 80), "rewarded_ad");
                       }, () =>
                       {
                           // Callback when ad fails to show
                           Debug.Log("Failed to show rewarded ad.");
                       });
    }
}
