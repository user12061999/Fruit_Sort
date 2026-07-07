using System;
using UnityEngine;
using HAVIGAME;
using HAVIGAME.UI;
using HAVIGAME.Services.Advertisings;
using HAVIGAME.Services.IAP;

public class GameAdvertising : Singleton<GameAdvertising> {
    public static readonly AdFilter defaultAdFilter = new AdFilter(AdNetwork.All, AdGroup.Group_1 | AdGroup.Tier_All);
    public static readonly AdFilter allAdFilter = new AdFilter(AdNetwork.All, AdGroup.Group_All | AdGroup.Tier_All);
    public static readonly AdFilter defaultCanvasNativeImmersiveAdFilter = new AdFilter(AdNetwork.All, AdGroup.Group_2 | AdGroup.Tier_All);
    public static readonly AdFilter appOpenInterstitialAdFilter = new AdFilter(AdNetwork.All, AdGroup.Group_2 | AdGroup.Tier_All);
    public static readonly AdSizeOption defaultNativeOverlayAdSize = new AdSizeOption(AdSizeType.AnchoredAdaptive);

    private static float lastTimeAppOpenAdShowed = float.MinValue;
    private static float lastTimeInterstitialAdShowed = float.MinValue;
    private static float lastTimeRewardedAdShowed = float.MinValue;
    private static float lastTimeRewardedInterstitialAdShowed = float.MinValue;

    private static Coroutine bannerAdRefreshCoroutine;


    public static bool IsInitialized() => AdvertisingManager.IsInitialized;
    public static bool IsAllServiceInitialized() => AdvertisingManager.IsInitialized && AdvertisingManager.IsAllServiceInitialized;


    public static bool IsAppOpenAdEnable => GameRemoteConfig.AppOpenAdEnable;
    public static bool IsIntertitialAdEnable => GameRemoteConfig.InterstitialAdEnable;
    public static bool IsAppOpenIntertitialAdEnable => GameRemoteConfig.AppOpenInterstitialAdEnable;
    public static bool IsRewaredAdEnable => GameRemoteConfig.RewardedAdEnable;
    public static bool IsRewaredInterstitialAdEnable => GameRemoteConfig.RewardedInterstitialAdEnable;
    public static bool IsBannerAdEnable => GameRemoteConfig.BannerAdEnable;
    public static bool IsMRectAdEnable => GameRemoteConfig.MRectAdEnable;
    public static bool IsNativeAdEnable => GameRemoteConfig.NativeAdEnable;
    public static bool IsNativeOverlayAdEnable => GameRemoteConfig.NativeOverlayAdEnable;
    public static bool IsNativeImmersiveAdEnable => GameRemoteConfig.NativeImmersiveAdEnable;


    public static float BannerAdRefreshTime => GameRemoteConfig.BannerAdRefreshTime;


    public static bool IsAppOpenAdReady => AdvertisingManager.IsAppOpenAdReady(defaultAdFilter);
    public static bool IsInterstitialAdReady => AdvertisingManager.IsInterstitialAdReady(defaultAdFilter);
    public static bool IsAppOpenInterstitialAdReady => AdvertisingManager.IsInterstitialAdReady(appOpenInterstitialAdFilter);
    public static bool IsRewardedAdReady => AdvertisingManager.IsRewardedAdReady(defaultAdFilter);
    public static bool IsRewardedInterstitialAdReady => AdvertisingManager.IsRewardedInterstitialAdReady(defaultAdFilter);
    public static bool IsBannerAdReady => AdvertisingManager.IsBannerAdReady(defaultAdFilter);
    public static bool IsMRectAdReady => AdvertisingManager.IsMediumRectangleAdReady(defaultAdFilter);
    public static bool IsNativeAdReady => AdvertisingManager.IsNativeAdReady(defaultAdFilter);
    public static bool IsNativeOverlayAdReady => AdvertisingManager.IsNativeOverlayAdReady(defaultAdFilter);


    public static bool IsShowingAppOpenAd => AdvertisingManager.IsAppOpenAdShowing(defaultAdFilter);
    public static bool IsShowingInterstitialAd => AdvertisingManager.IsInterstitialAdShowing(defaultAdFilter);
    public static bool IsShowingAppOpenInterstitialAd => AdvertisingManager.IsInterstitialAdShowing(appOpenInterstitialAdFilter);
    public static bool IsShowingRewardedAd => AdvertisingManager.IsRewardedAdShowing(defaultAdFilter);
    public static bool IsShowingRewardedInterstitialAd => AdvertisingManager.IsRewardedInterstitialAdShowing(defaultAdFilter);
    public static bool IsShowingBannerAd => AdvertisingManager.IsBannerAdShowing(defaultAdFilter);
    public static bool IsShowingMRectAd => AdvertisingManager.IsMediumRectangleAdShowing(defaultAdFilter);
    public static bool IsShowingNativeAd => AdvertisingManager.IsNativeAdShowing(defaultAdFilter);
    public static bool IsShowingNativeOverlayAd => AdvertisingManager.IsNativeOverlayAdShowing(defaultAdFilter);


    public static float LastTimeAppOpenAdShowed => lastTimeAppOpenAdShowed;
    public static float LastTimeInterstitialAdShowed => lastTimeInterstitialAdShowed;
    public static float LastTimeRewardedAdShowed => lastTimeRewardedAdShowed;
    public static float LastTimeRewardedInterstitialAdShowed => lastTimeRewardedInterstitialAdShowed;


    public static bool IsAppOpenAdCooldown => Time.time < lastTimeAppOpenAdShowed + GameRemoteConfig.AppOpenAdCooldownTime;
    public static bool IsInterstitialAdCooldown => Time.time < lastTimeInterstitialAdShowed + GameRemoteConfig.InterstitialAdCooldownTime;


    public static GameAdPosition GetBannerAdPosition() {
        return ToAdPosition(AdvertisingManager.GetBannerAdPosition(defaultAdFilter));
    }

    public static float GetBannerHeight() {
        if (IsShowingBannerAd) {
#if UNITY_EDITOR
            switch (GetBannerAdPosition()) {
                case GameAdPosition.TopCenter:
                    return 100;
                case GameAdPosition.TopLeft:
                    return 100;
                case GameAdPosition.TopRight:
                    return 100;
                case GameAdPosition.BottomCenter:
                    return 168;
                case GameAdPosition.BottomLeft:
                    return 168;
                case GameAdPosition.BottomRight:
                    return 168;
                default:
                    return 0;
            }
#else
            return AdvertisingManager.GetBannerAdHeight(defaultAdFilter);
#endif
        }

        return 0;
    }

    public static GameAdPosition GetMRectAdPosition() {
        return ToAdPosition(AdvertisingManager.GetMediumRectangleAdPosition(defaultAdFilter));
    }

    public static float GetMRectHeight() {
        if (IsShowingMRectAd) {
#if UNITY_EDITOR
            switch (GetMRectAdPosition()) {
                case GameAdPosition.TopCenter:
                    return 250;
                case GameAdPosition.TopLeft:
                    return 250;
                case GameAdPosition.TopRight:
                    return 250;
                case GameAdPosition.BottomCenter:
                    return 250;
                case GameAdPosition.BottomLeft:
                    return 250;
                case GameAdPosition.BottomRight:
                    return 250;
                default:
                    return 0;
            }
#else
            return AdvertisingManager.GetMediumRectangleAdHeight(defaultAdFilter);
#endif
        }

        return 0;
    }

    public static GameAdPosition GetNativeOverlayAdPosition() {
        return ToAdPosition(AdvertisingManager.GetNativeOverlayAdPosition(defaultAdFilter));
    }

    public static float GetNativeOverlayAdHeight() {
        if (IsShowingNativeOverlayAd) {
#if UNITY_EDITOR
            switch (GetNativeOverlayAdPosition()) {
                case GameAdPosition.TopCenter:
                    return 100;
                case GameAdPosition.TopLeft:
                    return 100;
                case GameAdPosition.TopRight:
                    return 100;
                case GameAdPosition.BottomCenter:
                    return 168;
                case GameAdPosition.BottomLeft:
                    return 168;
                case GameAdPosition.BottomRight:
                    return 168;
                default:
                    return 0;
            }
#else
            return AdvertisingManager.GetNativeOverlayAdHeight(defaultAdFilter);
#endif
        }

        return 0;
    }

    protected override void OnAwake() {
        RegisterAdEvents();
    }

    protected override void OnDestroy() {
        base.OnDestroy();

        UnregisterAdEvents();
    }

    private void OnApplicationPause(bool pause) {
        if (!pause) {
            if (GameIAP.IsTransacting) {
                Log.Warning("[GameAds] IAP is transacting.");
                return;
            }


            if (IsShowingInterstitialAd) {
                Log.Warning("[GameAds] Interstitial ad is showing.");
                return;
            }


            if (IsShowingRewardedAd) {
                Log.Warning("[GameAds] Rewarded ad is showing.");
                return;
            }


            if (IsShowingAppOpenInterstitialAd) {
                Log.Warning("[GameAds] App open interstitial ad is showing.");
                return;
            }

            bool bannerIsShowing = IsShowingBannerAd;
            GameAdPosition bannerAdPosition = GetBannerAdPosition();

            if (TryShowAppOpenAd(() => {
                if (bannerIsShowing) TryShowBannerAd(bannerAdPosition);
            })) {
                if (bannerIsShowing) TryHideBannerAd();
            }
        }
    }

    #region Ad Events
    private void RegisterAdEvents() {
        AdvertisingManager.onAdRevenuePaid += OnAdRevenuePaid;

        AdvertisingManager.onAdLoad += OnAdLoad;
        AdvertisingManager.onAdLoaded += OnAdLoaded;
        AdvertisingManager.onAdLoadFailed += OnAdLoadFailed;
        AdvertisingManager.onAdDisplay += OnAdDisplay;
        AdvertisingManager.onAdDisplayed += OnAdDisplayed;
        AdvertisingManager.onAdDisplayFailed += OnAdDisplayFailed;
        AdvertisingManager.onAdClicked += OnAdClicked;
        AdvertisingManager.onAdClosed += OnAdClosed;

        IAPManager.initializeEvent.AddListener(OnIAPInitialize);
    }

    private void UnregisterAdEvents() {
        AdvertisingManager.onAdRevenuePaid -= OnAdRevenuePaid;

        AdvertisingManager.onAdLoad -= OnAdLoad;
        AdvertisingManager.onAdLoaded -= OnAdLoaded;
        AdvertisingManager.onAdLoadFailed -= OnAdLoadFailed;
        AdvertisingManager.onAdDisplay -= OnAdDisplay;
        AdvertisingManager.onAdDisplayed -= OnAdDisplayed;
        AdvertisingManager.onAdDisplayFailed -= OnAdDisplayFailed;
        AdvertisingManager.onAdClicked -= OnAdClicked;
        AdvertisingManager.onAdClosed -= OnAdClosed;
    }

    public static bool IsRemoveAds() {
        return GameData.Player.IsPremium;
    }

    private void OnIAPInitialize(bool isInitialized) {
        GameData.Player.SetPremium(GameIAP.IsRemoveAds);
    }

    private void OnAdRevenuePaid(AdService client, AdFormat adFormat, AdRevenuePaid adRevenuePaid) {
        if (Log.DebugEnabled) {
            Log.Debug($"[GameAdvertising][{client.Network}] ad revenue paid: {adRevenuePaid}");
        }

        GameAnalytics.LogAdRevenue(new GameAnalytics.GameAdRevenue(adRevenuePaid.adNetwork.ToString(), adRevenuePaid.adSource, adRevenuePaid.adUnitId, GetAdFormatName(adRevenuePaid.adFormat), adRevenuePaid.value, adRevenuePaid.currency));

        GameAnalytics.LogEvent(GameAnalytics.GameEvent.Create("ad_revenue_paid_event")
            .Add("ad_source", adRevenuePaid.adSource)
            .Add("ad_network", adRevenuePaid.adNetwork)
            .Add("ad_format", adRevenuePaid.adFormat)
            .Add("ad_unit_id", adRevenuePaid.adUnitId)
            .Add("value", adRevenuePaid.value)
            .Add("currency", adRevenuePaid.currency));
    }

    private void OnAdLoad(AdEventArgs args) {
        LogAdEvent("load", args);
    }
    private void OnAdLoaded(AdEventArgs args) {
        LogAdEvent("load_finish", args, true);
    }
    private void OnAdLoadFailed(AdEventArgs args) {
        LogAdEvent("load_finish", args, false);
    }
    private void OnAdDisplay(AdEventArgs args) {
        LogAdEvent("display", args);
    }
    private void OnAdDisplayed(AdEventArgs args) {
        LogAdEvent("display_finish", args, true);
    }
    private void OnAdDisplayFailed(AdEventArgs args) {
        LogAdEvent("display_finish", args, false);
    }
    private void OnAdClicked(AdEventArgs args) {
        LogAdEvent("clicked", args);
    }
    private void OnAdClosed(AdEventArgs args) {
        LogAdEvent("closed", args);
    }

    private void LogAdEvent(string eventType, AdEventArgs args) {
        if (Log.DebugEnabled) {
            Log.Debug($"[GameAdvertising][{args.AdNetwork}-{args.AdFormat}] {eventType}, id = {args.AdUnitId}, placement = {args.Placement}, error = {args.Error}");
        }

        GameAnalytics.LogEvent(GameAnalytics.GameEvent.Create(Utility.Text.Format("{0}_{1}", GetAdFormatName(args.AdFormat), eventType))
            .Add("ad_network", args.AdNetwork.ToString())
            .Add("ad_format", args.AdFormat.ToString())
            .Add("ad_unit_id", args.AdUnitId)
            .Add("placement", args.Placement)
            .Add("error", args.Error));
    }

    private void LogAdEvent(string eventType, AdEventArgs args, bool completed) {
        if (Log.DebugEnabled) {
            Log.Debug($"[GameAdvertising][{args.AdNetwork}-{args.AdFormat}] {eventType}, id = {args.AdUnitId}, placement = {args.Placement}, error = {args.Error}");
        }

        GameAnalytics.LogEvent(GameAnalytics.GameEvent.Create(Utility.Text.Format("{0}_{1}", GetAdFormatName(args.AdFormat), eventType))
            .Add("ad_network", args.AdNetwork.ToString())
            .Add("ad_format", args.AdFormat.ToString())
            .Add("ad_unit_id", args.AdUnitId)
            .Add("completed", completed.ToString())
            .Add("placement", args.Placement)
            .Add("error", args.Error));
    }

    private string GetAdFormatName(AdFormat adUnit) {
        switch (adUnit) {
            case AdFormat.AppOpenAd: return "app_open";
            case AdFormat.BannerAd: return "banner";
            case AdFormat.RewardedAd: return "rewarded";
            case AdFormat.InterstitialAd: return "interstitial";
            case AdFormat.RewardedInterstitialAd: return "rewarded_interstitial";
            case AdFormat.MediumRectangleAd: return "medium_rectangle";
            case AdFormat.NativeAd: return "native";
            case AdFormat.NativeOverlayAd: return "native_overlay";
            default: return "unknow";
        }
    }
    #endregion

    public static void RemoveAds() {
        TryHideBannerAd();
        TryHideMediumRectangleAd();
        AdDisplayer.DestroyAll();
    }

    public static bool TryShowAppOpenAd(Action onCompleted = null, bool ignoreCooldownTime = false) {
        if (IsRemoveAds()) return false;

        GameAnalytics.LogEvent(GameAnalytics.GameEvent.Create("app_open_ad_request"));

#if AD_DISABLE
        return false;
#endif

#if CHEAT
        onCompleted?.Invoke();
        return true;
#endif

        if (!IsAppOpenAdEnable) {
            Log.Warning("[GameAds] App open ad is disabled");
            return false;
        }

        if (IsShowingAppOpenAd) {
            Log.Warning("[GameAds] App open ad is showing");
            return false;
        }

        if (!ignoreCooldownTime && IsAppOpenAdCooldown) {
            Log.Warning("[GameAds] App open ad is cooldown");
            return false;
        }

        if (AdvertisingManager.ShowAppOpenAd(onCompleted, GetCurrentPlacement(), defaultAdFilter)) {
            lastTimeAppOpenAdShowed = Time.time;
            return true;
        } else {
            Log.Warning("[GameAds] App open ad is not ready.");
            return false;
        }
    }


    public static bool TryShowInterstitialAd(Action onCompleted = null, bool ignoreCooldownTime = false) {
        if (IsRemoveAds()) return false;

        GameAnalytics.LogEvent(GameAnalytics.GameEvent.Create("interstitial_ad_request"));

#if AD_DISABLE
        return false;
#endif

#if CHEAT
        onCompleted?.Invoke();
        return true;
#endif


        if (!IsIntertitialAdEnable) {
            Log.Warning("[GameAds] Interstitial ad is disabled");
            return false;
        }

        if (IsShowingInterstitialAd) {
            Log.Warning("[GameAds] Interstitial ad is showing");
            return false;
        }

        if (!ignoreCooldownTime && IsInterstitialAdCooldown) {
            Log.Warning("[GameAds] Interstitial ad is cooldown");
            return false;
        }

        if (GameRemoteConfig.InterstitialAdLevelMin > GameData.Classic.LevelUnlocked) 
        {
            Log.Warning("[GameAds] Interstitial ad is not available for current level: " + GameData.Classic.LevelUnlocked);
            return false;
        }

        if (AdvertisingManager.ShowInterstitialAd(onCompleted, GetCurrentPlacement(), defaultAdFilter)) {
            lastTimeInterstitialAdShowed = Time.time;
            FixAppOpenAdCooldown();
            return true;
        } else {
            Log.Warning("[GameAds] Intertitial ad is not ready.");
            return false;
        }
    }


    public static bool TryShowAppOpenInterstitialAd(Action onCompleted = null, bool ignoreCooldownTime = false) {
        if (IsRemoveAds()) return false;

        GameAnalytics.LogEvent(GameAnalytics.GameEvent.Create("app_open_interstitial_ad_request"));

#if AD_DISABLE
        return false;
#endif

#if CHEAT
        onCompleted?.Invoke();
        return true;
#endif

        if (!IsAppOpenIntertitialAdEnable) {
            Log.Warning("[GameAds] App Open Interstitial ad is disabled");
            return false;
        }

        if (IsShowingAppOpenInterstitialAd) {
            Log.Warning("[GameAds] App Open Interstitial ad is showing");
            return false;
        }

        if (!ignoreCooldownTime && IsInterstitialAdCooldown) {
            Log.Warning("[GameAds] App Open Interstitial ad is cooldown");
            return false;
        }

        if (AdvertisingManager.ShowInterstitialAd(onCompleted, GetCurrentPlacement(), appOpenInterstitialAdFilter)) {
            lastTimeInterstitialAdShowed = Time.time;
            FixAppOpenAdCooldown();
            return true;
        } else {
            Log.Warning("[GameAds] App Open Intertitial ad is not ready.");
            return false;
        }
    }


    public static bool TryShowRewardedAd(Action onCompleted = null, Action onSkipped = null, bool showWarningPopup = true) {
        GameAnalytics.LogEvent(GameAnalytics.GameEvent.Create("rewarded_ad_request"));


#if AD_DISABLE
        return false;
#endif

#if CHEAT
        onCompleted?.Invoke();
        return true;
#endif

        if (!IsRewaredAdEnable) {
            Log.Warning("[GameAds] Reward ad is disabled");
            return false;
        }

        if (IsShowingRewardedAd) {
            Log.Warning("[GameAds] Reward Ad is showing");
            return false;
        }


        if (AdvertisingManager.ShowRewardedAd(onCompleted, onSkipped, GetCurrentPlacement(), defaultAdFilter)) {
            lastTimeRewardedAdShowed = Time.time;
            FixAppOpenAdCooldown();
            return true;
        } else {
            if (showWarningPopup) ShowPopupAdFailed("Reward ad no ready. Please try again later.");
            Log.Warning("[GameAds] Reward ad is not ready.");
            return false;
        }
    }


    public static bool TryShowRewardedInterstitialAd(Action onCompleted = null, Action onSkipped = null, bool showWarningPopup = true) {
        GameAnalytics.LogEvent(GameAnalytics.GameEvent.Create("rewarded_interstitial_ad_request"));

#if AD_DISABLE
        return false;
#endif

#if CHEAT
        onCompleted?.Invoke();
        return true;
#endif

        if (!IsRewaredInterstitialAdEnable) {
            Log.Warning("[GameAds] Reward interstitial ad is disabled");
            return false;
        }

        if (IsShowingRewardedInterstitialAd) {
            Log.Warning("[GameAds] Reward interstitial ad is showing");
            return false;
        }

        if (AdvertisingManager.ShowRewardedInterstitialAd(onCompleted, onSkipped, GetCurrentPlacement(), defaultAdFilter)) {
            lastTimeRewardedInterstitialAdShowed = Time.time;
            FixAppOpenAdCooldown();
            return true;
        } else {
            if (showWarningPopup) ShowPopupAdFailed("Reward interstitial ad no ready. Please try again later.");
            Log.Warning("[GameAds] Reward interstitial ad is not ready.");
            return false;
        }
    }


    public static bool TryShowBannerAd(GameAdPosition position) {
        return TryShowBannerAd(position, Vector2Int.zero);
    }

    public static bool TryShowBannerAd(GameAdPosition position, Vector2Int offset) {
        if (IsRemoveAds()) return false;

        GameAnalytics.LogEvent(GameAnalytics.GameEvent.Create("banner_ad_request"));

#if AD_DISABLE
        return false;
#endif

#if CHEAT
        return true;
#endif

        if (!IsBannerAdEnable) {
            Log.Warning("[GameAds] Banner ad is disabled");
            return false;
        }

        if (IsShowingBannerAd) {
            Log.Warning("[GameAds] Banner Ad is showing");
            return false;
        }

        if (AdvertisingManager.ShowBannerAd(FromAdPosition(position), offset, GetCurrentPlacement(), defaultAdFilter)) {
            float bannerAdRefreshTime = BannerAdRefreshTime;
            if (bannerAdRefreshTime > 0) {
                if (bannerAdRefreshCoroutine != null) Executor.Instance.Stop(bannerAdRefreshCoroutine);
                bannerAdRefreshCoroutine = Executor.Instance.Run(() => TryShowBannerAd(position, offset), bannerAdRefreshTime, false);
            }
            return true;
        } else {
            Log.Warning("[GameAds] Banner ad is not ready.");
            return false;
        }
    }

    public static bool TryHideBannerAd() {
        if (bannerAdRefreshCoroutine != null) Executor.Instance.Stop(bannerAdRefreshCoroutine);
        return AdvertisingManager.HideBannerAd(defaultAdFilter);
    }


    public static bool TryShowMediumRectangleAd(GameAdPosition position) {
        return TryShowMediumRectangleAd(position, Vector2Int.zero);
    }

    public static bool TryShowMediumRectangleAd(GameAdPosition position, Vector2Int offset) {
        if (IsRemoveAds()) return false;

        GameAnalytics.LogEvent(GameAnalytics.GameEvent.Create("mrect_ad_request"));

#if AD_DISABLE
        return false;
#endif

#if CHEAT
        return true;
#endif


        if (!IsMRectAdEnable) {
            Log.Warning("[GameAds] MRect ad is disabled");
            return false;
        }

        if (IsShowingMRectAd) {
            Log.Warning("[GameAds] MRect Ad is showing");
            return false;
        }

        if (AdvertisingManager.ShowMediumRectangleAd(FromAdPosition(position), offset, GetCurrentPlacement(), defaultAdFilter)) {
            return true;
        } else {
            Log.Warning("[GameAds] MRect ad is not ready.");
            return false;
        }
    }

    public static bool TryHideMediumRectangleAd() {
        return AdvertisingManager.HideMediumRectangleAd(defaultAdFilter);
    }


    public static bool TryGetNativeAd(out NativeAd nativeAd) {
        nativeAd = null;

        if (IsRemoveAds()) return false;

#if AD_DISABLE
        return false;
#endif

#if CHEAT
        return true;
#endif

        if (!IsNativeAdEnable) {
            Log.Warning("[GameAds] Native ad is disabled");
            return false;
        }

        if (AdvertisingManager.TryGetNativeAd(defaultAdFilter, out nativeAd)) {
            return true;
        } else if (AdvertisingManager.TryCreateNativeAd(defaultAdFilter, out nativeAd)) {
            return true;
        } else {
            Log.Warning("[GameAds] Create native ad failed.");
            return false;
        }
    }

    public static bool TryGetNativeAd(AdFilter adFilter, out NativeAd nativeAd) {
        nativeAd = null;

        if (IsRemoveAds()) return false;

#if AD_DISABLE
        return false;
#endif

#if CHEAT
        return true;
#endif

        if (!IsNativeAdEnable) {
            Log.Warning("[GameAds] Native ad is disabled");
            return false;
        }

        if (AdvertisingManager.TryGetNativeAd(adFilter, out nativeAd)) {
            return true;
        } else if (AdvertisingManager.TryCreateNativeAd(adFilter, out nativeAd)) {
            return true;
        } else {
            Log.Warning("[GameAds] Create native ad failed.");
            return false;
        }
    }


    public static bool TryGetNativeOverlayAd(out NativeOverlayAd nativeAd) {
        nativeAd = null;

        if (IsRemoveAds()) return false;

#if AD_DISABLE
        return false;
#endif

#if CHEAT
        return true;
#endif

        if (!IsNativeOverlayAdEnable) {
            Log.Warning("[GameAds] Native overlay ad is disabled");
            return false;
        }

        if (AdvertisingManager.TryGetNativeOverlayAd(defaultAdFilter, out nativeAd)) {
            return true;
        } else if (AdvertisingManager.TryCreateNativeOverlayAd(defaultAdFilter, out nativeAd)) {
            return true;
        } else {
            Log.Warning("[GameAds] Create overlay native ad failed.");
            return false;
        }
    }

    public static bool TryGetNativeOverlayAd(AdFilter adFilter, out NativeOverlayAd nativeAd) {
        nativeAd = null;

        if (IsRemoveAds()) return false;

#if AD_DISABLE
        return false;
#endif

#if CHEAT
        return true;
#endif

        if (!IsNativeOverlayAdEnable) {
            Log.Warning("[GameAds] Native overlay ad is disabled");
            return false;
        }

        if (AdvertisingManager.TryGetNativeOverlayAd(adFilter, out nativeAd)) {
            return true;
        } else if (AdvertisingManager.TryCreateNativeOverlayAd(adFilter, out nativeAd)) {
            return true;
        } else {
            Log.Warning("[GameAds] Create overlay native ad failed.");
            return false;
        }
    }


    public static bool TryGetNativeImmersiveAd(out NativeImmersiveAd nativeAd) {
        nativeAd = null;

        if (IsRemoveAds()) return false;

#if AD_DISABLE
        return false;
#endif

#if CHEAT
        return true;
#endif

        if (!IsNativeImmersiveAdEnable) {
            Log.Warning("[GameAds] Native immersive ad is disabled");
            return false;
        }

        if (AdvertisingManager.TryGetNativeImmersiveAd(defaultAdFilter, out nativeAd)) {
            return true;
        } else if (AdvertisingManager.TryCreateNativeImmersiveAd(defaultAdFilter, out nativeAd)) {
            return true;
        } else {
            Log.Warning("[GameAds] Create immersive native ad failed.");
            return false;
        }
    }

    public static bool TryGetNativeImmersiveAd(AdFilter adFilter, out NativeImmersiveAd nativeAd) {
        nativeAd = null;

        if (IsRemoveAds()) return false;

#if AD_DISABLE
        return false;
#endif

#if CHEAT
        return true;
#endif

        if (!IsNativeImmersiveAdEnable) {
            Log.Warning("[GameAds] Native immersive ad is disabled");
            return false;
        }

        if (AdvertisingManager.TryGetNativeImmersiveAd(adFilter, out nativeAd)) {
            return true;
        } else if (AdvertisingManager.TryCreateNativeImmersiveAd(adFilter, out nativeAd)) {
            return true;
        } else {
            Log.Warning("[GameAds] Create immersive native ad failed.");
            return false;
        }
    }


    public static void FixAppOpenAdCooldown() {
        float newLastTimeAppOpenAdShowed = Time.time - GameRemoteConfig.AppOpenAdCooldownTime + 1;

        if (lastTimeAppOpenAdShowed < newLastTimeAppOpenAdShowed) {
            lastTimeAppOpenAdShowed = newLastTimeAppOpenAdShowed;
        }
    }

    public static void ResetInterstitialAdCooldown() {
        lastTimeInterstitialAdShowed = float.MinValue;
    }

    public static void IncreaseInterstitialAdCooldown(float increaseSeconds = 15) {
        lastTimeInterstitialAdShowed += increaseSeconds;
    }

    private static GameAdPosition ToAdPosition(AdPositions position) {
        switch (position) {
            case AdPositions.TopCenter: return GameAdPosition.TopCenter;
            case AdPositions.TopLeft: return GameAdPosition.TopLeft;
            case AdPositions.TopRight: return GameAdPosition.TopRight;
            case AdPositions.Centered: return GameAdPosition.Centered;
            case AdPositions.CenterLeft: return GameAdPosition.CenterLeft;
            case AdPositions.CenterRight: return GameAdPosition.CenterRight;
            case AdPositions.BottomCenter: return GameAdPosition.BottomCenter;
            case AdPositions.BottomLeft: return GameAdPosition.BottomLeft;
            case AdPositions.BottomRight: return GameAdPosition.BottomRight;
            default: return GameAdPosition.Centered;
        }
    }

    private static AdPositions FromAdPosition(GameAdPosition position) {
        switch (position) {
            case GameAdPosition.TopCenter: return AdPositions.TopCenter;
            case GameAdPosition.TopLeft: return AdPositions.TopLeft;
            case GameAdPosition.TopRight: return AdPositions.TopRight;
            case GameAdPosition.Centered: return AdPositions.Centered;
            case GameAdPosition.CenterLeft: return AdPositions.CenterLeft;
            case GameAdPosition.CenterRight: return AdPositions.CenterRight;
            case GameAdPosition.BottomCenter: return AdPositions.BottomCenter;
            case GameAdPosition.BottomLeft: return AdPositions.BottomLeft;
            case GameAdPosition.BottomRight: return AdPositions.BottomRight;
            default: return AdPositions.Centered;
        }
    }

    public enum GameAdPosition {
        TopCenter,
        TopLeft,
        TopRight,
        Centered,
        CenterLeft,
        CenterRight,
        BottomCenter,
        BottomLeft,
        BottomRight,
    }

    private static string GetCurrentPlacement() {
        if (UIManager.HasInstance) {
            UIFrame frame = UIManager.Instance.Peek();

            if (frame) {
                return frame.GetType().Name;
            }
        }

        return "NULL";
    }

    private static void ShowPopupAdFailed(string message) {
        if (UIManager.HasInstance) {
            UIManager.Instance.Push<DialogPanel>().Dialog("Failed!", message);
        }
    }
}

