using System.Collections.Generic;
using HAVIGAME.Services.RemoteConfig;
using SimpleJSON;

public static class GameRemoteConfig {
    private static readonly Dictionary<string, JSONNode> jsonConfigs = new Dictionary<string, JSONNode>();

    private const string appOpenAdEnableKey = "app_open_ad_enable";
    private const string rewardedAdEnableKey = "rewarded_ad_enable";
    private const string rewardedInterstitialAdEnableKey = "rewarded_interstitial_ad_enable";
    private const string bannerAdEnableKey = "banner_ad_enable";
    private const string mrectAdEnableKey = "mrect_ad_enable";
    private const string nativeAdEnableKey = "native_ad_enable";
    private const string nativeOverlayAdEnableKey = "native_overlay_ad_enable";
    private const string nativeImmersiveAdEnableKey = "native_immersive_ad_enable";
    private const string interstitialAdEnableKey = "interstitial_ad_enable";
    private const string appOpenInterstitialAdEnableKey = "app_open_interstitial_ad_enable";
    private const string interstitialAdUIEnableKey = "interstitial_ad_ui_enable";
    private const string interstitialAdLevelMinKey = "interstitial_ad_level_min";

    private const string appOpenAdCooldownKey = "app_open_ad_cooldown_time";
    private const string interstitialAdCooldownTimeKey = "interstitial_ad_cooldown_time";
    private const string bannerAdRefreshTimeKey = "banner_ad_refresh_time";

    private const string rateFilterKey = "rate_filter";
    private const string appOpenLoadingDuration = "app_open_loading_duration";

    
    private const string interstitialAdCappingTimeKey = "interstitial_ad_capping_time";
    public static int InterstitialAdCappingTime {
        get {
            return GetIntValue(interstitialAdCappingTimeKey, 30);
        }
    }
    public static int RateFilter {
        get {
            return GetIntValue(rateFilterKey, 4);
        }
    }

    public static int AppOpenLoadingDuration {
        get {
            return GetIntValue(appOpenLoadingDuration, 7);
        }
    }

    public static bool AppOpenAdEnable {
        get {
            return GetBooleanValue(appOpenAdEnableKey, true);
        }
    }

    public static bool InterstitialAdEnable {
        get {
            return GetBooleanValue(interstitialAdEnableKey, true);
        }
    }

    public static bool AppOpenInterstitialAdEnable {
        get {
            return GetBooleanValue(appOpenInterstitialAdEnableKey, true);
        }
    }

    public static bool InterstitialAdUIEnable {
        get {
            return GetBooleanValue(interstitialAdUIEnableKey, true);
        }
    }

    public static int InterstitialAdCooldownTime {
        get {
            return GetIntValue(interstitialAdCooldownTimeKey, 30);
        }
    }

    public static int InterstitialAdLevelMin {
        get {
            return GetIntValue(interstitialAdLevelMinKey, 4);
        }
    }

    public static int BannerAdRefreshTime {
        get {
            return GetIntValue(bannerAdRefreshTimeKey, 120);
        }
    }

    public static bool RewardedAdEnable {
        get {
            return GetBooleanValue(rewardedAdEnableKey, true);
        }
    }

    public static bool RewardedInterstitialAdEnable {
        get {
            return GetBooleanValue(rewardedInterstitialAdEnableKey, true);
        }
    }

    public static bool BannerAdEnable {
        get {
            return GetBooleanValue(bannerAdEnableKey, true);
        }
    }

    public static bool MRectAdEnable {
        get {
            return GetBooleanValue(mrectAdEnableKey, true);
        }
    }

    public static bool NativeAdEnable {
        get {
            return GetBooleanValue(nativeAdEnableKey, true);
        }
    }
    public static bool NativeOverlayAdEnable {
        get {
            return GetBooleanValue(nativeOverlayAdEnableKey, true);
        }
    }
    public static bool NativeImmersiveAdEnable {
        get {
            return GetBooleanValue(nativeImmersiveAdEnableKey, true);
        }
    }
    public static float AppOpenAdCooldownTime {
        get {
            return GetIntValue(appOpenAdCooldownKey, 30);
        }
    }



    public static string GetStringValue(string key, string defaultValue = "") {
        return RemoteConfigManager.GetStringValue(key, defaultValue);
    }

    public static int GetIntValue(string key, int defaultValue = 0) {
        return RemoteConfigManager.GetIntValue(key, defaultValue);
    }

    public static bool GetBooleanValue(string key, bool defaultValue = false) {
        return RemoteConfigManager.GetBooleanValue(key, defaultValue);
    }

    public static float GetFloatValue(string key, float defaultValue = 0) {
        return RemoteConfigManager.GetFloatValue(key, defaultValue);
    }


    public static bool HasDataNode(string key, string path) {
        if (RemoteConfigManager.IsInitialized) {

            JSONNode dataNode;
            if (!jsonConfigs.TryGetValue(key, out dataNode)) {
                string remoteData = GetStringValue(key, null);

                if (!string.IsNullOrEmpty(remoteData)) {
                    dataNode = JSONNode.Parse(remoteData);
                    jsonConfigs[key] = dataNode;
                }
            }

            if (dataNode != null && !dataNode.IsNull) {
                JSONNode resultNode = GetNodeFormPath(dataNode, path);
                if (resultNode != null) {
                    return true;
                }
            }
        }

        return false;
    }

    public static string GetStringValue(string key, string path, string defaultValue = "") {
        if (RemoteConfigManager.IsInitialized) {

            JSONNode dataNode;
            if (!jsonConfigs.TryGetValue(key, out dataNode)) {
                string remoteData = GetStringValue(key, null);

                if (!string.IsNullOrEmpty(remoteData)) {
                    dataNode = JSONNode.Parse(remoteData);
                    jsonConfigs[key] = dataNode;
                }
            }

            if (dataNode != null && !dataNode.IsNull) {
                JSONNode resultNode = GetNodeFormPath(dataNode, path);

                if (resultNode != null) {
                    return resultNode.Value;
                }
            }
        }

        return defaultValue;
    }

    public static int GetIntValue(string key, string path, int defaultValue = 0) {
        if (RemoteConfigManager.IsInitialized) {

            JSONNode dataNode;
            if (!jsonConfigs.TryGetValue(key, out dataNode)) {
                string remoteData = GetStringValue(key, null);

                if (!string.IsNullOrEmpty(remoteData)) {
                    dataNode = JSONNode.Parse(remoteData);
                    jsonConfigs[key] = dataNode;
                }
            }

            if (dataNode != null && !dataNode.IsNull) {
                JSONNode resultNode = GetNodeFormPath(dataNode, path);

                if (resultNode != null) {
                    return resultNode.AsInt;
                }
            }
        }

        return defaultValue;
    }

    public static bool GetBooleanValue(string key, string path, bool defaultValue = false) {
        if (RemoteConfigManager.IsInitialized) {

            JSONNode dataNode;
            if (!jsonConfigs.TryGetValue(key, out dataNode)) {
                string remoteData = GetStringValue(key, null);

                if (!string.IsNullOrEmpty(remoteData)) {
                    dataNode = JSONNode.Parse(remoteData);
                    jsonConfigs[key] = dataNode;
                }
            }

            if (dataNode != null && !dataNode.IsNull) {
                JSONNode resultNode = GetNodeFormPath(dataNode, path);

                if (resultNode != null) {
                    return resultNode.AsBool;
                }
            }
        }

        return defaultValue;
    }

    public static float GetFloatValue(string key, string path, float defaultValue = 0) {
        if (RemoteConfigManager.IsInitialized) {

            JSONNode dataNode;
            if (!jsonConfigs.TryGetValue(key, out dataNode)) {
                string remoteData = GetStringValue(key, null);

                if (!string.IsNullOrEmpty(remoteData)) {
                    dataNode = JSONNode.Parse(remoteData);
                    jsonConfigs[key] = dataNode;
                }
            }

            if (dataNode != null && !dataNode.IsNull) {
                JSONNode resultNode = GetNodeFormPath(dataNode, path);

                if (resultNode != null) {
                    return resultNode.AsFloat;
                }
            }
        }

        return defaultValue;
    }

    private static JSONNode GetNodeFormPath(JSONNode node, string path) {
        string[] paths = path.Split('.');

        JSONNode currentNode = node;
        JSONNode resultNode = null;

        foreach (var item in paths) {
            if (currentNode.HasKey(item)) {
                resultNode = currentNode[item];
                currentNode = resultNode;
            }
        }

        return resultNode;
    }
}
