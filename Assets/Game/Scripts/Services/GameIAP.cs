using HAVIGAME.Services.IAP;
using System;

public static class GameIAP {
    public const string remove_ads = "com.game.removeads";

    public static bool IsInitialized => IAPManager.IsInitialized;
    public static bool IsTransacting => IAPManager.IsTransacting;

    public static bool IsRemoveAds => IsOwned(remove_ads);

    static GameIAP() {
        IAPManager.onPurchased += IAPManager_onPurchased;
        IAPManager.onRestorePurchased += IAPManager_onRestorePurchased;
    }

    private static void IAPManager_onPurchased(string productId) {

    }

    private static void IAPManager_onRestorePurchased(string[] productIds) {

    }

    public static bool IsOwned(string productId) {
        return IAPManager.IsOwned(productId);
    }

    public static bool IsSubscribed(string productId) {
        return IAPManager.IsSubscribed(productId);
    }

    public static IAPProduct GetProduct(string productId) {
        return IAPSettings.Instance.GetProduct(productId);
    }

    public static bool Purchase(string productId, Action onCompleted = null, Action<string> onFailed = null) {
#if CHEAT
        onCompleted?.Invoke();
        return true;
#else
        if (IAPManager.Purchase(productId, () => {
            decimal value = GetLocalizedPrice(productId);
            string currency = GetIsoCurrencyCode(productId);

            GameAnalytics.LogIAPRevenue(new GameAnalytics.GameIAPRevenue(productId, 1, value, currency));

            GameAnalytics.LogEvent(GameAnalytics.GameEvent.Create("in_app_purchase_event")
                .Add("product_id", productId)
                .Add("quantity", 1)
                .Add("value", value.ToString())
                .Add("currency", currency.ToString()));

            GameAdvertising.FixAppOpenAdCooldown();
            onCompleted?.Invoke();
        }, onFailed)) {
            GameAdvertising.FixAppOpenAdCooldown();
            return true;
        } else {
            return false;
        }
#endif
    }

    public static void RestorePurchases(Action<string[]> onCompleted = null, Action<string> onFailed = null) {
        IAPManager.RestorePurchases(onCompleted, onFailed);
    }

    public static string GetLocalizedPriceString(string productId) {
        return IAPManager.GetLocalizedPriceString(productId);
    }

    public static decimal GetLocalizedPrice(string productId) {
        return IAPManager.GetLocalizedPrice(productId);
    }

    public static string GetIsoCurrencyCode(string productId) {
        return IAPManager.GetIsoCurrencyCode(productId);
    }
}
