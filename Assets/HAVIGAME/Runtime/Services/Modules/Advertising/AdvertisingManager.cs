using System;
using System.Collections.Generic;
using UnityEngine;
using HAVIGAME.SaveLoad;

#if PRIVACY
using HAVIGAME.Services.Privacy;
#endif

namespace HAVIGAME.Services.Advertisings {

    public static class AdvertisingManager {

        public const string DEFINE_SYMBOL = "ADVERTISING";

        public static readonly InitializeEvent initializeEvent = new InitializeEvent();

        private static AdService[] adServices = null;
        private static DataHolder<AdvertisingSaveData> dataHolder;
        private static bool runOnMainTheard;
        private static AdFormat removeAdFormats;

        public static bool IsInitialized => initializeEvent.IsInitialized;
        public static bool IsRemoveAds => dataHolder != null ? dataHolder.Data.IsRemoveAds : false;
        public static AdFormat RemoveAdFormats => removeAdFormats;

        public static bool IsAllServiceInitialized {
            get {
                foreach (var service in adServices) {
                    if (!service.IsInitialized) return false;
                }
                return true;
            }
        }

        public static void Initialize() {
#if ADVERTISING
            if (initializeEvent.IsRunning) {
                Log.Warning("[Advertising] Cancel initialize! Advertising initialized with result {0}.", initializeEvent.IsInitialized);
                return;
            }

            AdvertisingSettings settings = AdvertisingSettings.Instance;

            runOnMainTheard = settings.ForceRunOnMainTheard;
            removeAdFormats = settings.RemoveAdFormats;

            if (settings.SaveData) {
                dataHolder = SaveLoadManager.Create<AdvertisingSaveData>(settings.SaveId);
            }

            if (IsRemoveAds) {
                Log.Info("[Advertising] Advertising initialize with remove ads flag.");
            }

#if PRIVACY
            InitializeAdvertisingsWithPrivacy();
#else
            InitializeAdvertisings();
#endif

#endif
        }

#if PRIVACY
        private static void InitializeAdvertisingsWithPrivacy() {
            PrivacyManager.initializeEvent.AddListener(OnPrivacyInitialized);
        }

        private static void OnPrivacyInitialized(bool isInitialized) {
            if (isInitialized) {
                PrivacyManager.RequestAuthorization(OnRequestAuthorizationCallback);
            }
            else {
                InitializeAdvertisings();
            }
        }

        private static void OnRequestAuthorizationCallback(AuthorizationStatus authorizationStatus) {
            InitializeAdvertisings();
        }
#endif

        private static void InitializeAdvertisings() {
            AdvertisingSettings settings = AdvertisingSettings.Instance;

            AdServiceProvider[] providers = settings.GetServiceProviders();

            if (providers.Length <= 0) {
                Log.Error("[Advertising] Initialize failed! Advertising services is empty.");
                initializeEvent.Invoke(false);
                return;
            }

            adServices = new AdService[providers.Length];

            for (int i = 0; i < providers.Length; i++) {
                adServices[i] = providers[i].GetService();
                RegisterClientEvents(adServices[i]);
                adServices[i].Initialize();
            }

            Database.Unload(settings);

            Log.Info("[Advertising] Initialize completed.");
            initializeEvent.Invoke(true);
        }


        public static AdService GetAdService(AdNetwork network) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize.");
                return null;
            }

            foreach (var item in adServices) {
                if (item.Network == network) return item;
            }
            return null;
        }

        public static void SetRemoveAds(bool isRemoveAds) {
            if (dataHolder != null) {
                dataHolder.Data.SetRemoveAds(isRemoveAds);
            } else {
                Log.Warning("[Advertising] Set ad removal status failed! Please enable ads data saving in settings.");
            }
        }

        #region Events
        public static event AdClientInitializeDelegate onAdInitialized;
        public static event AdClientRevenuePaidDelegate onAdRevenuePaid;
        public static event AdDelegate onAdLoad;
        public static event AdDelegate onAdLoaded;
        public static event AdDelegate onAdLoadFailed;
        public static event AdDelegate onAdDisplay;
        public static event AdDelegate onAdDisplayed;
        public static event AdDelegate onAdDisplayFailed;
        public static event AdDelegate onAdClicked;
        public static event AdDelegate onAdClosed;

        private static void RegisterClientEvents(AdService client) {
            client.initializeEvent.AddListener(isInitialized => InvokeOnAdInitializedEvent(client, isInitialized));
            client.onAdRevenuePaid += InvokeOnAdRevenuePaidEvent;
            client.onAdLoad += InvokeOnAdLoadEvent;
            client.onAdLoaded += InvokeOnAdLoadedEvent;
            client.onAdLoadFailed += InvokeOnAdLoadFailedEvent;
            client.onAdDisplay += InvokeOnAdDisplayEvent;
            client.onAdDisplayed += InvokeOnAdDisplayedEvent;
            client.onAdDisplayFailed += InvokeOnAdDisplayFailedEvent;
            client.onAdClicked += InvokeOnAdClickedEvent;
            client.onAdClosed += InvokeOnAdClosedEvent;
        }
        private static void UnregisterClientEvents(AdService client) {
            client.onAdRevenuePaid -= InvokeOnAdRevenuePaidEvent;
            client.onAdLoad -= InvokeOnAdLoadEvent;
            client.onAdLoaded -= InvokeOnAdLoadedEvent;
            client.onAdLoadFailed -= InvokeOnAdLoadFailedEvent;
            client.onAdDisplay -= InvokeOnAdDisplayEvent;
            client.onAdDisplayed -= InvokeOnAdDisplayedEvent;
            client.onAdDisplayFailed -= InvokeOnAdDisplayFailedEvent;
            client.onAdClicked -= InvokeOnAdClickedEvent;
            client.onAdClosed -= InvokeOnAdClosedEvent;
        }

        private static void InvokeOnAdInitializedEvent(AdService client, bool isInitialized) {
            if (runOnMainTheard) {
                Invoke(() => onAdInitialized?.Invoke(client, isInitialized));
            } else {
                onAdInitialized?.Invoke(client, isInitialized);
            }
        }
        private static void InvokeOnAdRevenuePaidEvent(AdService client, AdFormat adFormat, AdRevenuePaid adImpression) {
            if (runOnMainTheard) {
                Invoke(() => onAdRevenuePaid?.Invoke(client, adFormat, adImpression));
            } else {
                onAdRevenuePaid?.Invoke(client, adFormat, adImpression);
            }
        }
        private static void InvokeOnAdLoadEvent(AdEventArgs args) {
            if (runOnMainTheard) {
                Invoke(() => {
                    onAdLoad?.Invoke(args);
                    ReferencePool.Release(args);
                });
            } else {
                onAdLoad?.Invoke(args);
                ReferencePool.Release(args);
            }
        }
        private static void InvokeOnAdLoadedEvent(AdEventArgs args) {
            if (runOnMainTheard) {
                Invoke(() => {
                    onAdLoaded?.Invoke(args);
                    ReferencePool.Release(args);
                });
            } else {
                onAdLoaded?.Invoke(args);
                ReferencePool.Release(args);
            }
        }
        private static void InvokeOnAdLoadFailedEvent(AdEventArgs args) {
            if (runOnMainTheard) {
                Invoke(() => {
                    onAdLoadFailed?.Invoke(args);
                    ReferencePool.Release(args);
                });
            } else {
                onAdLoadFailed?.Invoke(args);
                ReferencePool.Release(args);
            }
        }
        private static void InvokeOnAdDisplayEvent(AdEventArgs args) {
            if (runOnMainTheard) {
                Invoke(() => {
                    onAdDisplay?.Invoke(args);
                    ReferencePool.Release(args);
                });
            } else {
                onAdDisplay?.Invoke(args);
                ReferencePool.Release(args);
            }
        }
        private static void InvokeOnAdDisplayedEvent(AdEventArgs args) {
            if (runOnMainTheard) {
                Invoke(() => {
                    onAdDisplayed?.Invoke(args);
                    ReferencePool.Release(args);
                });
            } else {
                onAdDisplayed?.Invoke(args);
                ReferencePool.Release(args);
            }
        }
        private static void InvokeOnAdDisplayFailedEvent(AdEventArgs args) {
            if (runOnMainTheard) {
                Invoke(() => {
                    onAdDisplayFailed?.Invoke(args);
                    ReferencePool.Release(args);
                });
            } else {
                onAdDisplayFailed?.Invoke(args);
                ReferencePool.Release(args);
            }
        }
        private static void InvokeOnAdClickedEvent(AdEventArgs args) {
            if (runOnMainTheard) {
                Invoke(() => {
                    onAdClicked?.Invoke(args);
                    ReferencePool.Release(args);
                });
            } else {
                onAdClicked?.Invoke(args);
                ReferencePool.Release(args);
            }
        }
        private static void InvokeOnAdClosedEvent(AdEventArgs args) {
            if (runOnMainTheard) {
                Invoke(() => {
                    onAdClosed?.Invoke(args);
                    ReferencePool.Release(args);
                });
            } else {
                onAdClosed?.Invoke(args);
                ReferencePool.Release(args);
            }
        }
        private static void Invoke(Action action) {
            if (Executor.Instance) Executor.Instance.RunOnMainTheard(action);
        }
        #endregion

        #region App Open Ad
        public static AppOpenAd GetAppOpenAd(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return null;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsAppOpenAdInitialized) {
                    AppOpenAd result = adService.AppOpenAds.Get(filter.Group);

                    if (result != null) {
                        return result;
                    }
                }
            }

            return null;
        }

        public static IEnumerable<AppOpenAd> GetAppOpenAds(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                yield break;
            } else {
                foreach (AdService adService in adServices) {
                    if (adService.IsInNetwork(filter.Network) && adService.IsAppOpenAdInitialized) {
                        foreach (var item in adService.AppOpenAds.GetAll(filter.Group)) {
                            yield return item;
                        }
                    }
                }
            }
        }

        public static bool IsAppOpenAdReady(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsAppOpenAdInitialized && adService.AppOpenAds.IsReady(filter.Group)) {
                    return true;
                }
            }

            return false;
        }

        public static bool IsAppOpenAdShowing(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsAppOpenAdInitialized && adService.AppOpenAds.IsShowing(filter.Group)) {
                    return true;
                }
            }

            return false;
        }

        public static bool ShowAppOpenAd(Action onCompleted, string placement, AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsAppOpenAdInitialized && adService.AppOpenAds.TryGetReady(filter.Group, out AppOpenAd ad) && ad.Show(onCompleted, placement)) {
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Interstitial Ad
        public static InterstitialAd GetInterstitialAd(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return null;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsInterstitialAdInitialized) {
                    InterstitialAd result = adService.InterstitialAds.Get(filter.Group);

                    if (result != null) {
                        return result;
                    }
                }
            }

            return null;
        }

        public static IEnumerable<InterstitialAd> GetInterstitialAds(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                yield break;
            } else {
                foreach (AdService adService in adServices) {
                    if (adService.IsInNetwork(filter.Network) && adService.IsInterstitialAdInitialized) {
                        foreach (var item in adService.InterstitialAds.GetAll(filter.Group)) {
                            yield return item;
                        }
                    }
                }
            }
        }

        public static bool IsInterstitialAdReady(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsInterstitialAdInitialized && adService.InterstitialAds.IsReady(filter.Group)) {
                    return true;
                }
            }

            return false;
        }

        public static bool IsInterstitialAdShowing(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsInterstitialAdInitialized && adService.InterstitialAds.IsShowing(filter.Group)) {
                    return true;
                }
            }

            return false;
        }

        public static bool ShowInterstitialAd(Action onCompleted, string placement, AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsInterstitialAdInitialized && adService.InterstitialAds.TryGetReady(filter.Group, out InterstitialAd ad) && ad.Show(onCompleted, placement)) {
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Rewarded Ad
        public static RewardedAd GetRewardedAd(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return null;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsRewardedAdInitialized) {
                    RewardedAd result = adService.RewardedAds.Get(filter.Group);

                    if (result != null) {
                        return result;
                    }
                }
            }

            return null;
        }

        public static IEnumerable<RewardedAd> GetRewardedAds(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                yield break;
            } else {
                foreach (AdService adService in adServices) {
                    if (adService.IsInNetwork(filter.Network) && adService.IsRewardedAdInitialized) {
                        foreach (var item in adService.RewardedAds.GetAll(filter.Group)) {
                            yield return item;
                        }
                    }
                }
            }
        }

        public static bool IsRewardedAdReady(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsRewardedAdInitialized && adService.RewardedAds.IsReady(filter.Group)) {
                    return true;
                }
            }

            return false;
        }

        public static bool IsRewardedAdShowing(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsRewardedAdInitialized && adService.RewardedAds.IsShowing(filter.Group)) {
                    return true;
                }
            }

            return false;
        }

        public static bool ShowRewardedAd(Action onCompleted, Action onFailed, string placement, AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsRewardedAdInitialized && adService.RewardedAds.TryGetReady(filter.Group, out RewardedAd ad) && ad.Show(onCompleted, onFailed, placement)) {
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Banner Ad
        public static BannerAd GetBannerAd(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return null;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsBannerAdInitialized) {
                    BannerAd result = adService.BannerAds.Get(filter.Group);

                    if (result != null) {
                        return result;
                    }
                }
            }

            return null;
        }

        public static IEnumerable<BannerAd> GetBannerAds(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                yield break;
            } else {
                foreach (AdService adService in adServices) {
                    if (adService.IsInNetwork(filter.Network) && adService.IsBannerAdInitialized) {
                        foreach (var item in adService.BannerAds.GetAll(filter.Group)) {
                            yield return item;
                        }
                    }
                }
            }
        }

        public static bool IsBannerAdReady(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsBannerAdInitialized && adService.BannerAds.IsReady(filter.Group)) {
                    return true;
                }
            }

            return false;
        }

        public static bool IsBannerAdShowing(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsBannerAdInitialized && adService.BannerAds.IsShowing(filter.Group)) {
                    return true;
                }
            }

            return false;
        }

        public static AdPositions GetBannerAdPosition(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return AdPositions.Null;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsBannerAdInitialized && adService.BannerAds.TryGetShowing(filter.Group, out BannerAd ad)) {
                    return ad.Position;
                }
            }

            return AdPositions.Null;
        }

        public static float GetBannerAdWidth(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return 0;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsBannerAdInitialized && adService.BannerAds.TryGetShowing(filter.Group, out BannerAd ad)) {
                    return ad.Width;
                }
            }

            return 0;
        }

        public static float GetBannerAdHeight(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return 0;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsBannerAdInitialized && adService.BannerAds.TryGetShowing(filter.Group, out BannerAd ad)) {
                    return ad.Height;
                }
            }

            return 0;
        }

        public static bool ShowBannerAd(AdPositions position, Vector2Int offset, string placement, AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsBannerAdInitialized && adService.BannerAds.TryGetReady(filter.Group, out BannerAd ad) && ad.Show(position, offset, placement)) {
                    return true;
                }
            }

            return false;
        }

        public static bool SetBannerAdPosition(AdPositions position, Vector2Int offset, AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsBannerAdInitialized && adService.BannerAds.TryGet(filter.Group, out BannerAd ad)) {
                    ad.SetPosition(position, offset);
                    return true;
                }
            }

            return false;
        }

        public static bool HideBannerAd(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsBannerAdInitialized && adService.BannerAds.TryGetShowing(filter.Group, out BannerAd ad) && ad.Hide()) {
                    return true;
                }
            }

            return false;
        }

        public static bool DestroyBannerAd(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsBannerAdInitialized && adService.BannerAds.TryGet(filter.Group, out BannerAd ad) && ad.Destroy()) {
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Native Ad
        public static NativeAd GetNativeAd(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return null;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsNativeAdInitialized) {
                    NativeAd result = adService.NativeAds.Get(filter.Group);

                    if (result != null) {
                        return result;
                    }
                }
            }

            return null;
        }

        public static IEnumerable<NativeAd> GetNativeAds(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                yield break;
            } else {
                foreach (AdService adService in adServices) {
                    if (adService.IsInNetwork(filter.Network) && adService.IsNativeAdInitialized) {
                        foreach (var item in adService.NativeAds.GetAll(filter.Group)) {
                            yield return item;
                        }
                    }
                }
            }
        }

        public static bool IsNativeAdReady(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsNativeAdInitialized && adService.NativeAds.IsReady(filter.Group)) {
                    return true;
                }
            }

            return false;
        }

        public static bool IsNativeAdShowing(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsNativeAdInitialized && adService.NativeAds.IsShowing(filter.Group)) {
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetNativeAd(AdFilter filter, out NativeAd nativeAd) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                nativeAd = null;
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsNativeAdInitialized) {
                    foreach (NativeAd ad in adService.NativeAds) {
                        if (ad.IsInGroup(filter.Group) && !ad.Owned) {
                            nativeAd = ad;
                            return true;
                        }
                    }
                }
            }

            nativeAd = null;
            return false;
        }

        public static bool TryCreateNativeAd(AdFilter filter, out NativeAd nativeAd) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                nativeAd = null;
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsNativeAdInitialized) {
                    foreach (NativeAd ad in adService.NativeAds) {
                        if (ad.IsInGroup(filter.Group)) {
                            nativeAd = adService.Add(AdFormat.NativeAd, ad.AdId.Clone()) as NativeAd;
                            return true;
                        }
                    }
                }
            }

            nativeAd = null;
            return false;
        }
        #endregion

        #region Medium Rectangle Ad
        public static MediumRectangleAd GetMediumRectangleAd(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return null;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsMediumRectangleAdInitialized) {
                    MediumRectangleAd result = adService.MediumRectangleAds.Get(filter.Group);

                    if (result != null) {
                        return result;
                    }
                }
            }

            return null;
        }

        public static IEnumerable<MediumRectangleAd> GetMediumRectangleAds(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                yield break;
            } else {
                foreach (AdService adService in adServices) {
                    if (adService.IsInNetwork(filter.Network) && adService.IsMediumRectangleAdInitialized) {
                        foreach (var item in adService.MediumRectangleAds.GetAll(filter.Group)) {
                            yield return item;
                        }
                    }
                }
            }
        }

        public static bool IsMediumRectangleAdReady(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsMediumRectangleAdInitialized && adService.MediumRectangleAds.IsReady(filter.Group)) {
                    return true;
                }
            }

            return false;
        }

        public static bool IsMediumRectangleAdShowing(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsMediumRectangleAdInitialized && adService.MediumRectangleAds.IsShowing(filter.Group)) {
                    return true;
                }
            }

            return false;
        }

        public static AdPositions GetMediumRectangleAdPosition(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return AdPositions.Null;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsMediumRectangleAdInitialized && adService.MediumRectangleAds.TryGetShowing(filter.Group, out MediumRectangleAd ad)) {
                    return ad.Position;
                }
            }

            return AdPositions.Null;
        }
        public static float GetMediumRectangleAdWidth(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return 0;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsMediumRectangleAdInitialized && adService.MediumRectangleAds.TryGetShowing(filter.Group, out MediumRectangleAd ad)) {
                    return ad.Width;
                }
            }

            return 0;
        }

        public static float GetMediumRectangleAdHeight(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return 0;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsMediumRectangleAdInitialized && adService.MediumRectangleAds.TryGetShowing(filter.Group, out MediumRectangleAd ad)) {
                    return ad.Height;
                }
            }

            return 0;
        }

        public static bool ShowMediumRectangleAd(AdPositions position, Vector2Int offset, string placement, AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsMediumRectangleAdInitialized && adService.MediumRectangleAds.TryGetReady(filter.Group, out MediumRectangleAd ad) && ad.Show(position, offset, placement)) {
                    return true;
                }
            }

            return false;
        }

        public static bool SetMediumRectangleAdPosition(AdPositions position, Vector2Int offset, AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsMediumRectangleAdInitialized && adService.MediumRectangleAds.TryGetShowing(filter.Group, out MediumRectangleAd ad)) {
                    ad.SetPosition(position, offset);
                    return true;
                }
            }

            return false;
        }

        public static bool HideMediumRectangleAd(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsMediumRectangleAdInitialized && adService.MediumRectangleAds.TryGetShowing(filter.Group, out MediumRectangleAd ad) && ad.Hide()) {
                    return true;
                }
            }

            return false;
        }

        public static bool DestroyMediumRectangleAd(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsMediumRectangleAdInitialized && adService.MediumRectangleAds.TryGet(filter.Group, out MediumRectangleAd ad) && ad.Destroy()) {
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Rewarded Interstitial Ad
        public static RewardedInterstitialAd GetRewardedInterstitialAd(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return null;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsRewardedInterstitialAdInitialized) {
                    RewardedInterstitialAd result = adService.RewardedInterstitialAds.Get(filter.Group);

                    if (result != null) {
                        return result;
                    }
                }
            }

            return null;
        }

        public static IEnumerable<RewardedInterstitialAd> GetRewardedInterstitialAds(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                yield break;
            } else {
                foreach (AdService adService in adServices) {
                    if (adService.IsInNetwork(filter.Network) && adService.IsRewardedInterstitialAdInitialized) {
                        foreach (var item in adService.RewardedInterstitialAds.GetAll(filter.Group)) {
                            yield return item;
                        }
                    }
                }
            }
        }

        public static bool IsRewardedInterstitialAdReady(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsRewardedInterstitialAdInitialized && adService.RewardedInterstitialAds.IsReady(filter.Group)) {
                    return true;
                }
            }

            return false;
        }

        public static bool IsRewardedInterstitialAdShowing(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsRewardedInterstitialAdInitialized && adService.RewardedInterstitialAds.IsShowing(filter.Group)) {
                    return true;
                }
            }

            return false;
        }

        public static bool ShowRewardedInterstitialAd(Action onCompleted, Action onFailed, string placement, AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsRewardedInterstitialAdInitialized && adService.RewardedInterstitialAds.TryGetReady(filter.Group, out RewardedInterstitialAd ad) && ad.Show(onCompleted, onFailed, placement)) {
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Native overlay Ad
        public static NativeOverlayAd GetNativeOverlayAd(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return null;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsNativeOverlayAdInitialized) {
                    NativeOverlayAd result = adService.NativeOverlayAds.Get(filter.Group);

                    if (result != null) {
                        return result;
                    }
                }
            }

            return null;
        }

        public static IEnumerable<NativeOverlayAd> GetNativeOverlayAds(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                yield break;
            } else {
                foreach (AdService adService in adServices) {
                    if (adService.IsInNetwork(filter.Network) && adService.IsNativeOverlayAdInitialized) {
                        foreach (var item in adService.NativeOverlayAds.GetAll(filter.Group)) {
                            yield return item;
                        }
                    }
                }
            }
        }

        public static bool IsNativeOverlayAdReady(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsNativeOverlayAdInitialized && adService.NativeOverlayAds.IsReady(filter.Group)) {
                    return true;
                }
            }

            return false;
        }

        public static bool IsNativeOverlayAdShowing(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsNativeOverlayAdInitialized && adService.NativeOverlayAds.IsShowing(filter.Group)) {
                    return true;
                }
            }

            return false;
        }

        public static AdPositions GetNativeOverlayAdPosition(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return AdPositions.Null;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsNativeOverlayAdInitialized && adService.NativeOverlayAds.TryGetShowing(filter.Group, out NativeOverlayAd ad)) {
                    return ad.Position;
                }
            }

            return AdPositions.Null;
        }

        public static float GetNativeOverlayAdWidth(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return 0;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsNativeOverlayAdInitialized && adService.NativeOverlayAds.TryGetShowing(filter.Group, out NativeOverlayAd ad)) {
                    return ad.Width;
                }
            }

            return 0;
        }

        public static float GetNativeOverlayAdHeight(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return 0;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsNativeOverlayAdInitialized && adService.NativeOverlayAds.TryGetShowing(filter.Group, out NativeOverlayAd ad)) {
                    return ad.Height;
                }
            }

            return 0;
        }

        public static bool TryGetNativeOverlayAd(AdFilter filter, out NativeOverlayAd nativeOverlayAd) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                nativeOverlayAd = null;
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsNativeOverlayAdInitialized) {
                    foreach (NativeOverlayAd ad in adService.NativeOverlayAds) {
                        if (ad.IsInGroup(filter.Group) && !ad.Owned) {
                            nativeOverlayAd = ad;
                            return true;
                        }
                    }
                }
            }

            nativeOverlayAd = null;
            return false;
        }

        public static bool TryCreateNativeOverlayAd(AdFilter filter, out NativeOverlayAd nativeOverlayAd) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                nativeOverlayAd = null;
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsNativeOverlayAdInitialized) {
                    foreach (NativeOverlayAd ad in adService.NativeOverlayAds) {
                        if (ad.IsInGroup(filter.Group)) {
                            nativeOverlayAd = adService.Add(AdFormat.NativeOverlayAd, ad.AdId.Clone()) as NativeOverlayAd;
                            return true;
                        }
                    }
                }
            }

            nativeOverlayAd = null;
            return false;
        }
        #endregion

        #region Native Immersive Ad
        public static NativeImmersiveAd GetNativeImmersiveAd(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return null;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsNativeImmersiveAdInitialized) {
                    NativeImmersiveAd result = adService.NativeImmersiveAds.Get(filter.Group);

                    if (result != null) {
                        return result;
                    }
                }
            }

            return null;
        }

        public static IEnumerable<NativeImmersiveAd> GetNativeImmersiveAds(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                yield break;
            } else {
                foreach (AdService adService in adServices) {
                    if (adService.IsInNetwork(filter.Network) && adService.IsNativeImmersiveAdInitialized) {
                        foreach (var item in adService.NativeImmersiveAds.GetAll(filter.Group)) {
                            yield return item;
                        }
                    }
                }
            }
        }

        public static bool IsNativeImmersiveAdReady(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsNativeImmersiveAdInitialized && adService.NativeImmersiveAds.IsReady(filter.Group)) {
                    return true;
                }
            }

            return false;
        }

        public static bool IsNativeImmersiveAdShowing(AdFilter filter) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsNativeImmersiveAdInitialized && adService.NativeImmersiveAds.IsShowing(filter.Group)) {
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetNativeImmersiveAd(AdFilter filter, out NativeImmersiveAd nativeImmersiveAd) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                nativeImmersiveAd = null;
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsNativeImmersiveAdInitialized) {
                    foreach (NativeImmersiveAd ad in adService.NativeImmersiveAds) {
                        if (ad.IsInGroup(filter.Group) && !ad.Owned) {
                            nativeImmersiveAd = ad;
                            return true;
                        }
                    }
                }
            }

            nativeImmersiveAd = null;
            return false;
        }

        public static bool TryCreateNativeImmersiveAd(AdFilter filter, out NativeImmersiveAd nativeImmersiveAd) {
            if (!IsInitialized) {
                Log.Warning("[Advertising] Advertising no initialize!");
                nativeImmersiveAd = null;
                return false;
            }

            foreach (AdService adService in adServices) {
                if (adService.IsInNetwork(filter.Network) && adService.IsNativeImmersiveAdInitialized) {
                    foreach (NativeImmersiveAd ad in adService.NativeImmersiveAds) {
                        if (ad.IsInGroup(filter.Group)) {
                            nativeImmersiveAd = adService.Add(AdFormat.NativeImmersiveAd, ad.AdId.Clone()) as NativeImmersiveAd;
                            return true;
                        }
                    }
                }
            }

            nativeImmersiveAd = null;
            return false;
        }
        #endregion


#if ADVERTISING
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {

            public override int Order => SERVICE;
            public override InitializeEvent InitializeEvent => AdvertisingManager.initializeEvent;
            public override void Initialize() {
                AdvertisingManager.Initialize();
            }
        }
#endif
    }
}
