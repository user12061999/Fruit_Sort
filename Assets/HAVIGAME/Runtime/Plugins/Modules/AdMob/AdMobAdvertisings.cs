using System;
using UnityEngine;
using System.Collections.Generic;
using HAVIGAME.Services.Advertisings;

#if ADMOB
using GoogleMobileAds.Api;

#if ADMOB_CUSTOM
using GoogleMobileAds.NativeCustom;
#endif

#endif

namespace HAVIGAME.Plugins.AdMob {

#if ADMOB
    public class AdMobAdvertisings : AdService {
        private AdSizeOption bannerAdSize;

        public override HAVIGAME.Services.Advertisings.AdNetwork Network => AdNetwork.AdMob;
        public override Services.Advertisings.AdFormat AdFormatSupported {
            get {
                Services.Advertisings.AdFormat adUnitSupported = Services.Advertisings.AdFormat.AdMobAdUnits;

#if ADMOB_NATIVE
                adUnitSupported |= Services.Advertisings.AdFormat.NativeAd;
#endif

#if ADMOB_NATIVE_IMMERSIVE
                adUnitSupported |= Services.Advertisings.AdFormat.NativeImmersiveAd;
#endif

                return adUnitSupported;
            }
        }

        protected override Services.Advertisings.AppOpenAd CreateAppOpenAd(AdId id) {
            AdMobAppOpenAd ad = new AdMobAppOpenAd(this, id.Clone());
            AppOpenAds.Add(ad);
            return ad;
        }

        protected override Services.Advertisings.BannerAd CreateBannerAd(AdId id) {
            AdMobBannerAd ad = new AdMobBannerAd(this, id.Clone(), AdMobSettings.Instance.CollapsibleBanner, bannerAdSize);
            BannerAds.Add(ad);
            return ad;
        }

        protected override Services.Advertisings.InterstitialAd CreateInterstitialAd(AdId id) {
            AdMobInterstitialAd ad = new AdMobInterstitialAd(this, id.Clone());
            InterstitialAds.Add(ad);
            return ad;
        }

        protected override Services.Advertisings.MediumRectangleAd CreateMediumRectangleAd(AdId id) {
            AdMobMediumRectangleAd ad = new AdMobMediumRectangleAd(this, id.Clone());
            MediumRectangleAds.Add(ad);
            return ad;
        }

        protected override Services.Advertisings.NativeAd CreateNativeAd(AdId id) {
#if ADMOB_NATIVE
            AdMobNativeAd ad = new AdMobNativeAd(this, id.Clone());
            NativeAds.Add(ad);
            return ad;
#else
            return null;
#endif
        }

        protected override Services.Advertisings.NativeImmersiveAd CreateNativeImmersiveAd(AdId id) {
#if ADMOB_NATIVE_IMMERSIVE
            AdMobNativeImmersiveAd ad = new AdMobNativeImmersiveAd(this, id.Clone());
            NativeImmersiveAds.Add(ad);
            return ad;
#else
            return null;
#endif
        }

        protected override Services.Advertisings.NativeOverlayAd CreateNativeOverlayAd(AdId id) {
            AdMobNativeOverlayAd ad = new AdMobNativeOverlayAd(this, id.Clone());
            NativeOverlayAds.Add(ad);
            return ad;
        }

        protected override Services.Advertisings.RewardedAd CreateRewardedAd(AdId id) {
            AdMobRewardedAd ad = new AdMobRewardedAd(this, id.Clone());
            RewardedAds.Add(ad);
            return ad;
        }

        protected override Services.Advertisings.RewardedInterstitialAd CreateRewardedInterstitialAd(AdId id) {
            AdMobRewardedInterstitialAd ad = new AdMobRewardedInterstitialAd(this, id.Clone());
            RewardedInterstitialAds.Add(ad);
            return ad;
        }

        public override void Initialize() {
            if (InitializeEvent.IsRunning) {
                Log.Warning("[AdMobAdvertisings] AdMob advertisings is running with initialize state {0}.", IsInitialized);
                return;
            }

            AdMobManager.initializeEvent.AddListener(OnAdMobInitializeCallback);
        }

        private void OnAdMobInitializeCallback(bool isInitialized) {
            if (isInitialized) {
                AdMobSettings settings = AdMobSettings.Instance;
                bannerAdSize = settings.BannerAdSize;
                uint preloadBufferSize = (uint)settings.PreloadBufferSize;
                bool preloadEnabled = preloadBufferSize > 0;
                List<PreloadConfiguration> preloadConfigurations = null;

                if (preloadEnabled) preloadConfigurations = new List<PreloadConfiguration>();

                if (!IsRemoveAdFormat(Services.Advertisings.AdFormat.AppOpenAd)) {
                    AppOpenAds = new AdCollection<Services.Advertisings.AppOpenAd>();

                    if (settings.AppOpenAdIds != null && settings.AppOpenAdIds.Length > 0) {
                        for (int i = 0; i < settings.AppOpenAdIds.Length; i++) {

                            if (preloadEnabled && settings.AppOpenAdIds[i].AutoLoadType == LoadType.MediationPreload) {
                                var appOpenAd = new AdMobAppOpenAdPreload(this, settings.AppOpenAdIds[i]);
                                PreloadConfiguration configuration = new PreloadConfiguration() {
                                    Format = GoogleMobileAds.Api.AdFormat.APP_OPEN_AD,
                                    AdUnitId = settings.AppOpenAdIds[i].Current(),
                                    BufferSize = preloadBufferSize,
                                };
                                preloadConfigurations.Add(configuration);
                                AppOpenAds.Add(appOpenAd);

                            } else {
                                var appOpenAd = new AdMobAppOpenAd(this, settings.AppOpenAdIds[i]);
                                AppOpenAds.Add(appOpenAd);
                            }
                        }
                    }
                }

                if (!IsRemoveAdFormat(Services.Advertisings.AdFormat.InterstitialAd)) {
                    InterstitialAds = new AdCollection<Services.Advertisings.InterstitialAd>();

                    if (settings.InterstitialAdIds != null && settings.InterstitialAdIds.Length > 0) {
                        for (int i = 0; i < settings.InterstitialAdIds.Length; i++) {
                            if (preloadEnabled && settings.InterstitialAdIds[i].AutoLoadType == LoadType.MediationPreload) {
                                var interstitialAd = new AdMobInterstitialAdPreload(this, settings.InterstitialAdIds[i]);
                                PreloadConfiguration configuration = new PreloadConfiguration() {
                                    Format = GoogleMobileAds.Api.AdFormat.INTERSTITIAL,
                                    AdUnitId = settings.InterstitialAdIds[i].Current(),
                                    BufferSize = preloadBufferSize,
                                };
                                preloadConfigurations.Add(configuration);
                                InterstitialAds.Add(interstitialAd);

                            } else {
                                var interstitialAd = new AdMobInterstitialAd(this, settings.InterstitialAdIds[i]);
                                InterstitialAds.Add(interstitialAd);
                            }
                        }
                    }
                }

                if (!IsRemoveAdFormat(Services.Advertisings.AdFormat.RewardedAd)) {
                    RewardedAds = new AdCollection<Services.Advertisings.RewardedAd>();

                    if (settings.RewardedAdIds != null && settings.RewardedAdIds.Length > 0) {
                        for (int i = 0; i < settings.RewardedAdIds.Length; i++) {
                            if (preloadEnabled && settings.RewardedAdIds[i].AutoLoadType == LoadType.MediationPreload) {
                                var rewardedAd = new AdMobRewardedAdPreload(this, settings.RewardedAdIds[i]);
                                PreloadConfiguration configuration = new PreloadConfiguration() {
                                    Format = GoogleMobileAds.Api.AdFormat.REWARDED,
                                    AdUnitId = settings.RewardedAdIds[i].Current(),
                                    BufferSize = preloadBufferSize,
                                };
                                preloadConfigurations.Add(configuration);
                                RewardedAds.Add(rewardedAd);

                            } else {
                                AdMobRewardedAd rewardedAd = new AdMobRewardedAd(this, settings.RewardedAdIds[i]);
                                RewardedAds.Add(rewardedAd);
                            }
                        }
                    }
                }

                if (!IsRemoveAdFormat(Services.Advertisings.AdFormat.BannerAd)) {
                    BannerAds = new AdCollection<Services.Advertisings.BannerAd>();

                    if (settings.BannerAdIds != null && settings.BannerAdIds.Length > 0) {
                        for (int i = 0; i < settings.BannerAdIds.Length; i++) {
                            AdMobBannerAd bannerAd = new AdMobBannerAd(this, settings.BannerAdIds[i], settings.CollapsibleBanner, bannerAdSize);
                            BannerAds.Add(bannerAd);
                        }
                    }
                }

                if (!IsRemoveAdFormat(Services.Advertisings.AdFormat.MediumRectangleAd)) {
                    MediumRectangleAds = new AdCollection<Services.Advertisings.MediumRectangleAd>();

                    if (settings.MediumRectangleAdIds != null && settings.MediumRectangleAdIds.Length > 0) {
                        for (int i = 0; i < settings.MediumRectangleAdIds.Length; i++) {
                            AdMobMediumRectangleAd mediumRectangleAd = new AdMobMediumRectangleAd(this, settings.MediumRectangleAdIds[i]);
                            MediumRectangleAds.Add(mediumRectangleAd);
                        }
                    }
                }

                if (!IsRemoveAdFormat(Services.Advertisings.AdFormat.RewardedInterstitialAd)) {

                    RewardedInterstitialAds = new AdCollection<Services.Advertisings.RewardedInterstitialAd>();

                    if (settings.RewardedInterstitialAdIds != null && settings.RewardedInterstitialAdIds.Length > 0) {
                        for (int i = 0; i < settings.RewardedInterstitialAdIds.Length; i++) {
                            AdMobRewardedInterstitialAd rewardedInterstitialAd = new AdMobRewardedInterstitialAd(this, settings.RewardedInterstitialAdIds[i]);
                            RewardedInterstitialAds.Add(rewardedInterstitialAd);
                        }
                    }
                }

#if ADMOB_NATIVE
                if (!IsRemoveAdFormat(Services.Advertisings.AdFormat.NativeAd)) {
                    NativeAds = new AdCollection<Services.Advertisings.NativeAd>();
                    
                    if (settings.NativeAdIds != null && settings.NativeAdIds.Length > 0) {
                        for (int i = 0; i < settings.NativeAdIds.Length; i++) {
                            AdMobNativeAd nativeAd = new AdMobNativeAd(this, settings.NativeAdIds[i]);
                            NativeAds.Add(nativeAd);
                        }
                    }
                }
#endif

                if (!IsRemoveAdFormat(Services.Advertisings.AdFormat.NativeOverlayAd)) {
                    NativeOverlayAds = new AdCollection<Services.Advertisings.NativeOverlayAd>();

                    if (settings.NativeOverlayAdIds != null && settings.NativeOverlayAdIds.Length > 0) {
                        for (int i = 0; i < settings.NativeOverlayAdIds.Length; i++) {
                            AdMobNativeOverlayAd nativeOverlayAd = new AdMobNativeOverlayAd(this, settings.NativeOverlayAdIds[i]);
                            NativeOverlayAds.Add(nativeOverlayAd);
                        }
                    }
                }

#if ADMOB_NATIVE_IMMERSIVE
                if (AdMobManager.IsImmersiveInGameDisplayAdsSupported) {
                    if (!IsRemoveAdFormat(Services.Advertisings.AdFormat.NativeImmersiveAd)) {
                        NativeImmersiveAds = new AdCollection<Services.Advertisings.NativeImmersiveAd>();

                        if (settings.NativeImmersiveAdIds != null && settings.NativeImmersiveAdIds.Length > 0) {
                            for (int i = 0; i < settings.NativeImmersiveAdIds.Length; i++) {
                                AdMobNativeImmersiveAd nativeImmersiveAd = new AdMobNativeImmersiveAd(this, settings.NativeImmersiveAdIds[i]);
                                NativeImmersiveAds.Add(nativeImmersiveAd);
                            }
                        }
                    }
                }
#endif

#if ADMOB_CUSTOM
                if (!IsRemoveAdFormat(Services.Advertisings.AdFormat.NativeAd)) {
                    GoogleMobileAdsCustom.Initialize(this);
                }
#endif

                if (preloadEnabled) {
                    MobileAds.Preload(preloadConfigurations, OnAdsAvailable, OnAdExhausted);
                }

                Log.Info("[AdMobAdvertisings] AdMob advertisings initialize completed.");
                InitializeEvent.Invoke(true);

                Database.Unload(settings);
            } else {
                Log.Error("[AdMobAdvertisings] AdMob advertisings initialize failed because AdMob initialize failed");
                InitializeEvent.Invoke(false);
            }
        }

        private void OnAdsAvailable(PreloadConfiguration preloadConfig) {
            if (Log.DebugEnabled) Log.Debug($"[AdMobAdvertisings] Preload ad for configuration {preloadConfig.Format} {preloadConfig.AdUnitId} is available.");

            switch (preloadConfig.Format) {
                case GoogleMobileAds.Api.AdFormat.BANNER:
                    break;
                case GoogleMobileAds.Api.AdFormat.INTERSTITIAL:
                    foreach (var item in InterstitialAds) {
                        if (item.Id.Equals(preloadConfig.AdUnitId) && item is AdMobInterstitialAdPreload ad) {
                            ad.InvokeOnLoadedEvent();
                            break;
                        }
                    }
                    break;
                case GoogleMobileAds.Api.AdFormat.REWARDED:
                    foreach (var item in RewardedAds) {
                        if (item.Id.Equals(preloadConfig.AdUnitId) && item is AdMobRewardedAdPreload ad) {
                            ad.InvokeOnLoadedEvent();
                            break;
                        }
                    }
                    break;
                case GoogleMobileAds.Api.AdFormat.REWARDED_INTERSTITIAL:
                    break;
                case GoogleMobileAds.Api.AdFormat.NATIVE:
                    break;
                case GoogleMobileAds.Api.AdFormat.APP_OPEN_AD:
                    foreach (var item in AppOpenAds) {
                        if (item.Id.Equals(preloadConfig.AdUnitId) && item is AdMobAppOpenAdPreload ad) {
                            ad.InvokeOnLoadedEvent();
                            break;
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private void OnAdExhausted(PreloadConfiguration preloadConfig) {
            if (Log.DebugEnabled) Log.Debug($"[AdMobAdvertisings] Preload ad for configuration {preloadConfig.Format} {preloadConfig.AdUnitId} is exhausted.");
        }

        private class AdMobAppOpenAd : Services.Advertisings.AppOpenAd {
            private AdMobAdvertisings client;
            private string placement;
            private Action onCompleted;
            private int retry;
            private GoogleMobileAds.Api.AppOpenAd ad;
            private DateTime loadTime;
            public override AdService Client => client;
            public override bool IsReady {
                get {
                    if (ad != null) {
                        if ((DateTime.UtcNow - loadTime).TotalHours >= 4) {
                            Log.Warning("[AdMobAppOpenAd] App open ad has expried!");
                            ad.Destroy();
                            ad = null;
                            if (HasAutoLoad(LoadType.OnExpried)) {
                                Reload(true);
                            }
                            return false;
                        } else {
                            bool canShow = ad.CanShowAd();

                            if (canShow) {
                                return true;
                            } else {
                                Log.Warning("[AdMobAppOpenAd] App open ad can only be shown once per load!");
                                ad.Destroy();
                                ad = null;
                                if (HasAutoLoad(LoadType.OnExpried)) {
                                    Reload(true);
                                }
                                return false;
                            }
                        }
                    }
                    return false;
                }
            }

            public AdMobAppOpenAd(AdMobAdvertisings client, AdId id) : base(id) {
                this.client = client;
                this.retry = 0;
                this.placement = "NULL";
                ad = null;

                if (HasAutoLoad(LoadType.OnCreated)) {
                    Load();
                }
            }

            public override Ad Clone() {
                return new AdMobAppOpenAd(client, AdId.Clone());
            }

            public override bool Load() {

                if (IsReady) {
                    Log.Warning("[AdMobAppOpenAd] Load failed! App open ad is ready!");
                    return false;
                }

                if (IsReloading) {
                    Log.Warning("[AdMobAppOpenAd] Load failed! App open ad is reloading!");
                    return false;
                }

                if (IsLoading) {
                    Log.Warning("[AdMobAppOpenAd] Load failed! App open ad is loading!");
                    return false;
                }

                if (string.IsNullOrEmpty(Id)) {
                    Log.Warning("[AdMobAppOpenAd] Load failed! App open ad id is null or empty!");
                    return false;
                }

                IsLoading = true;
                InvokeOnLoadEvent(placement);

                AdRequest request = new AdRequest();
                GoogleMobileAds.Api.AppOpenAd.Load(Id, request, this.OnAdLoadEvent);
                loadTime = DateTime.UtcNow;
                return true;
            }

            public override bool Show(Action onCompleted, string placement) {
                if (!IsReady) {
                    Log.Warning("[AdMobAppOpenAd] Show failed! App open ad not avaliable!");
                    return false;
                }

                if (IsShowing) {
                    Log.Warning("[AdMobAppOpenAd] Show failed! App open ad is showing!");
                    return false;
                }

                IsShowing = true;
                this.onCompleted = onCompleted;
                this.placement = placement;

                ad.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
                ad.OnAdFullScreenContentFailed += OnAdFullScreenContentFailed;
                ad.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
                ad.OnAdPaid += OnAdPaid;
                ad.OnAdImpressionRecorded += OnAdImpressionRecorded;

                InvokeOnDisplayEvent(placement);

                ad.Show();
                return true;
            }

            public override bool Destroy() {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;

                    if (IsShowing) {
                        IsShowing = false;

                        InvokeOnClosedEvent(placement);
                    }

                    if (HasAutoLoad(LoadType.OnDestroyed)) {
                        Reload(true);
                    }

                    return true;
                }

                return false;
            }

            private void OnAdLoadEvent(GoogleMobileAds.Api.AppOpenAd ad, LoadAdError error) {
                IsLoading = false;

                if (error == null) {
                    this.ad = ad;
                    InvokeOnLoadedEvent(placement);

                    retry = 0;
                    loadTime = DateTime.UtcNow;
                } else {
                    InvokeOnLoadFailedEvent(placement, error.GetMessage());

                    if (HasAutoLoad(LoadType.OnLoadFailed)) {
                        retry++;
                        float delay = 2 * Math.Min(6, retry);
                        IsReloading = true;
                        Invoke(Reload, delay);
                    }
                }
            }

            private void OnAdFullScreenContentOpened() {
                InvokeOnDisplayedEvent(placement);
            }

            private void OnAdPaid(AdValue value) {
                AdRevenuePaid adRevenuePaid = new AdRevenuePaid() {
                    adSource = client.Network.ToString(),
                    adNetwork = client.Network,
                    adFormat = Format,
                    adUnitId = Id,
                    placement = placement,
                    value = value.Value / 1000000d,
                    currency = value.CurrencyCode,
                };

                InvokeOnRevenuePaidEvent(adRevenuePaid);
            }

            private void OnAdFullScreenContentFailed(AdError error) {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;
                }

                IsShowing = false;
                Invoke(onCompleted);
                InvokeOnDisplayFailedEvent(placement, error.GetMessage());

                if (HasAutoLoad(LoadType.OnDisplayFailed)) {
                    Reload(true);
                }
            }

            private void OnAdImpressionRecorded() {
            }

            private void OnAdFullScreenContentClosed() {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;
                }

                IsShowing = false;
                Invoke(onCompleted);
                InvokeOnClosedEvent(placement);

                if (HasAutoLoad(LoadType.OnClosed)) {
                    Reload(true);
                }
            }
        }

        private class AdMobAppOpenAdPreload : Services.Advertisings.AppOpenAd {
            private AdMobAdvertisings client;
            private string placement;
            private Action onCompleted;
            private int retry;
            private DateTime loadTime;
            private GoogleMobileAds.Api.AppOpenAd ad;

            public override AdService Client => client;
            public override bool IsReady {
                get {
                    return GoogleMobileAds.Api.AppOpenAd.IsAdAvailable(Id);
                }
            }

            public AdMobAppOpenAdPreload(AdMobAdvertisings client, AdId id) : base(id) {
                this.client = client;
                this.retry = 0;
                this.placement = "NULL";
                ad = null;

                if (HasAutoLoad(LoadType.OnCreated)) {
                    Load();
                }
            }

            public override Ad Clone() {
                return new AdMobAppOpenAdPreload(client, AdId.Clone());
            }

            public override bool Load() {
                return false;
            }

            public override bool Show(Action onCompleted, string placement) {
                if (!IsReady) {
                    Log.Warning("[AdMobAppOpenAd] Show failed! App open ad not avaliable!");
                    return false;
                }

                if (IsShowing) {
                    Log.Warning("[AdMobAppOpenAd] Show failed! App open ad is showing!");
                    return false;
                }

                IsShowing = true;
                this.onCompleted = onCompleted;
                this.placement = placement;

                ad = GoogleMobileAds.Api.AppOpenAd.PollAd(Id);
                ad.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
                ad.OnAdFullScreenContentFailed += OnAdFullScreenContentFailed;
                ad.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
                ad.OnAdPaid += OnAdPaid;
                ad.OnAdImpressionRecorded += OnAdImpressionRecorded;

                InvokeOnDisplayEvent(placement);

                ad.Show();
                return true;
            }

            public override bool Destroy() {
                return false;
            }

            public void InvokeOnLoadedEvent() {
                InvokeOnLoadedEvent(placement);
            }

            private void OnAdLoadEvent(GoogleMobileAds.Api.AppOpenAd ad, LoadAdError error) {
                IsLoading = false;

                if (error == null) {
                    this.ad = ad;
                    InvokeOnLoadedEvent(placement);

                    retry = 0;
                    loadTime = DateTime.UtcNow;
                } else {
                    InvokeOnLoadFailedEvent(placement, error.GetMessage());

                    if (HasAutoLoad(LoadType.OnLoadFailed)) {
                        retry++;
                        float delay = 2 * Math.Min(6, retry);
                        IsReloading = true;
                        Invoke(Reload, delay);
                    }
                }
            }

            private void OnAdFullScreenContentOpened() {
                InvokeOnDisplayedEvent(placement);
            }

            private void OnAdPaid(AdValue value) {
                AdRevenuePaid adRevenuePaid = new AdRevenuePaid() {
                    adSource = client.Network.ToString(),
                    adNetwork = client.Network,
                    adFormat = Format,
                    adUnitId = Id,
                    placement = placement,
                    value = value.Value / 1000000d,
                    currency = value.CurrencyCode,
                };

                InvokeOnRevenuePaidEvent(adRevenuePaid);
            }

            private void OnAdFullScreenContentFailed(AdError error) {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;
                }

                IsShowing = false;
                Invoke(onCompleted);
                InvokeOnDisplayFailedEvent(placement, error.GetMessage());

                if (HasAutoLoad(LoadType.OnDisplayFailed)) {
                    Reload(true);
                }
            }

            private void OnAdImpressionRecorded() {
            }

            private void OnAdFullScreenContentClosed() {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;
                }

                IsShowing = false;
                Invoke(onCompleted);
                InvokeOnClosedEvent(placement);

                if (HasAutoLoad(LoadType.OnClosed)) {
                    Reload(true);
                }
            }
        }

        private class AdMobInterstitialAd : Services.Advertisings.InterstitialAd {
            private AdMobAdvertisings client;
            private string placement;
            private Action onCompleted;
            private int retry;
            private GoogleMobileAds.Api.InterstitialAd ad;
            public override AdService Client => client;
            public override bool IsReady {
                get {
                    if (ad != null) {
                        bool canShow = ad.CanShowAd();

                        if (canShow) {
                            return true;
                        } else {
                            Log.Warning("[AdMobInterstitialAd] Interstitial ad can only be shown once per load!");
                            ad.Destroy();
                            ad = null;
                            if (HasAutoLoad(LoadType.OnExpried)) {
                                Reload(true);
                            }
                            return false;
                        }
                    }
                    return false;
                }
            }

            public AdMobInterstitialAd(AdMobAdvertisings client, AdId id) : base(id) {
                this.client = client;
                this.retry = 0;
                this.placement = "NULL";
                ad = null;

                if (HasAutoLoad(LoadType.OnCreated)) {
                    Load();
                }
            }

            public override Ad Clone() {
                return new AdMobInterstitialAd(client, AdId.Clone());
            }

            public override bool Load() {
                if (IsReady) {
                    Log.Warning("[AdMobInterstitialAd] Load failed! Interstitial ad is ready!");
                    return false;
                }

                if (IsReloading) {
                    Log.Warning("[AdMobInterstitialAd] Load failed! Interstitial ad is reloading!");
                    return false;
                }

                if (IsLoading) {
                    Log.Warning("[AdMobInterstitialAd] Load failed! Interstitial ad is loading!");
                    return false;
                }

                if (string.IsNullOrEmpty(Id)) {
                    Log.Warning("[AdMobInterstitialAd] Load failed! Interstitial ad id is null or empty!");
                    return false;
                }


                IsLoading = true;

                InvokeOnLoadEvent(placement);

                AdRequest request = new AdRequest();
                GoogleMobileAds.Api.InterstitialAd.Load(Id, request, OnAdLoadEvent);
                return true;
            }

            public override bool Show(Action onCompleted, string placement) {
                if (!IsReady) {
                    Log.Warning("[AdMobInterstitialAd] Show failed! Interstitial ad not avaliable!");
                    return false;
                }

                if (IsShowing) {
                    Log.Warning("[AdMobInterstitialAd] Show failed! Interstitial ad is showing!");
                    return false;
                }

                IsShowing = true;
                this.onCompleted = onCompleted;
                this.placement = placement;

                ad.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
                ad.OnAdFullScreenContentFailed += OnAdFullScreenContentFailed;
                ad.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
                ad.OnAdClicked += OnAdClicked;
                ad.OnAdPaid += OnAdPaid;
                ad.OnAdImpressionRecorded += OnAdImpressionRecorded;

                InvokeOnDisplayEvent(placement);

                ad.Show();
                return true;
            }

            public override bool Destroy() {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;

                    if (IsShowing) {
                        IsShowing = false;

                        InvokeOnClosedEvent(placement);
                    }

                    if (HasAutoLoad(LoadType.OnDestroyed)) {
                        Reload(true);
                    }

                    return true;
                }

                return false;
            }

            private void OnAdLoadEvent(GoogleMobileAds.Api.InterstitialAd ad, LoadAdError error) {
                IsLoading = false;

                if (error == null) {
                    this.ad = ad;
                    InvokeOnLoadedEvent(placement);

                    retry = 0;
                } else {
                    InvokeOnLoadFailedEvent(placement, error.GetMessage());

                    if (HasAutoLoad(LoadType.OnLoadFailed)) {
                        retry++;
                        float delay = 2 * Math.Min(6, retry);
                        IsReloading = true;
                        Invoke(Reload, delay);
                    }
                }
            }

            private void OnAdFullScreenContentOpened() {
                InvokeOnDisplayedEvent(placement);
            }

            private void OnAdFullScreenContentFailed(AdError error) {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;
                }

                IsShowing = false;
                Invoke(onCompleted);
                InvokeOnDisplayFailedEvent(placement, error.GetMessage());

                if (HasAutoLoad(LoadType.OnDisplayFailed)) {
                    Reload(true);
                }
            }

            private void OnAdClicked() {
                InvokeOnClickedEvent(placement);
            }

            private void OnAdPaid(AdValue value) {
                AdRevenuePaid adRevenuePaid = new AdRevenuePaid() {
                    adSource = client.Network.ToString(),
                    adNetwork = client.Network,
                    adUnitId = Id,
                    adFormat = Format,
                    placement = placement,
                    value = value.Value / 1000000d,
                    currency = value.CurrencyCode,
                };

                InvokeOnRevenuePaidEvent(adRevenuePaid);
            }

            private void OnAdImpressionRecorded() {
            }

            private void OnAdFullScreenContentClosed() {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;
                }

                IsShowing = false;
                Invoke(onCompleted);
                InvokeOnClosedEvent(placement);

                if (HasAutoLoad(LoadType.OnClosed)) {
                    Reload(true);
                }
            }
        }

        private class AdMobInterstitialAdPreload : Services.Advertisings.InterstitialAd {
            private AdMobAdvertisings client;
            private string placement;
            private Action onCompleted;
            private int retry;
            private GoogleMobileAds.Api.InterstitialAd ad;
            public override AdService Client => client;
            public override bool IsReady {
                get {
                    return GoogleMobileAds.Api.InterstitialAd.IsAdAvailable(Id);
                }
            }

            public AdMobInterstitialAdPreload(AdMobAdvertisings client, AdId id) : base(id) {
                this.client = client;
                this.retry = 0;
                this.placement = "NULL";
                ad = null;
            }

            public override Ad Clone() {
                return new AdMobInterstitialAdPreload(client, AdId.Clone());
            }

            public override bool Load() {
                return false;
            }

            public override bool Show(Action onCompleted, string placement) {
                if (!IsReady) {
                    Log.Warning("[AdMobInterstitialAd] Show failed! Interstitial ad not avaliable!");
                    return false;
                }

                if (IsShowing) {
                    Log.Warning("[AdMobInterstitialAd] Show failed! Interstitial ad is showing!");
                    return false;
                }

                IsShowing = true;
                this.onCompleted = onCompleted;
                this.placement = placement;

                ad = GoogleMobileAds.Api.InterstitialAd.PollAd(Id);
                ad.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
                ad.OnAdFullScreenContentFailed += OnAdFullScreenContentFailed;
                ad.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
                ad.OnAdClicked += OnAdClicked;
                ad.OnAdPaid += OnAdPaid;
                ad.OnAdImpressionRecorded += OnAdImpressionRecorded;

                InvokeOnDisplayEvent(placement);

                ad.Show();
                return true;
            }

            public override bool Destroy() {
                return false;
            }

            public void InvokeOnLoadedEvent() {
                InvokeOnLoadedEvent(placement);
            }

            private void OnAdLoadEvent(GoogleMobileAds.Api.InterstitialAd ad, LoadAdError error) {
                IsLoading = false;

                if (error == null) {
                    this.ad = ad;
                    InvokeOnLoadedEvent(placement);

                    retry = 0;
                } else {
                    InvokeOnLoadFailedEvent(placement, error.GetMessage());

                    if (HasAutoLoad(LoadType.OnLoadFailed)) {
                        retry++;
                        float delay = 2 * Math.Min(6, retry);
                        IsReloading = true;
                        Invoke(Reload, delay);
                    }
                }
            }

            private void OnAdFullScreenContentOpened() {
                InvokeOnDisplayedEvent(placement);
            }

            private void OnAdFullScreenContentFailed(AdError error) {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;
                }

                IsShowing = false;
                Invoke(onCompleted);
                InvokeOnDisplayFailedEvent(placement, error.GetMessage());

                if (HasAutoLoad(LoadType.OnDisplayFailed)) {
                    Reload(true);
                }
            }

            private void OnAdClicked() {
                InvokeOnClickedEvent(placement);
            }

            private void OnAdPaid(AdValue value) {
                AdRevenuePaid adRevenuePaid = new AdRevenuePaid() {
                    adSource = client.Network.ToString(),
                    adNetwork = client.Network,
                    adUnitId = Id,
                    adFormat = Format,
                    placement = placement,
                    value = value.Value / 1000000d,
                    currency = value.CurrencyCode,
                };

                InvokeOnRevenuePaidEvent(adRevenuePaid);
            }

            private void OnAdImpressionRecorded() {
            }

            private void OnAdFullScreenContentClosed() {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;
                }

                IsShowing = false;
                Invoke(onCompleted);
                InvokeOnClosedEvent(placement);

                if (HasAutoLoad(LoadType.OnClosed)) {
                    Reload(true);
                }
            }
        }

        private class AdMobRewardedAd : Services.Advertisings.RewardedAd {
            private AdMobAdvertisings client;
            private string placement;
            private Action onCompleted;
            private Action onFailed;
            private int retry;
            private bool receivedReward;
            private GoogleMobileAds.Api.RewardedAd ad;

            public override AdService Client => client;
            public override bool IsReady {
                get {
                    if (ad != null) {
                        bool canShow = ad.CanShowAd();

                        if (canShow) {
                            return true;
                        } else {
                            Log.Warning("[AdMobRewardedAd] Rewarded ad can only be shown once per load!");
                            ad.Destroy();
                            ad = null;
                            if (HasAutoLoad(LoadType.OnExpried)) {
                                Reload(true);
                            }
                            return false;
                        }
                    }
                    return false;
                }
            }

            public AdMobRewardedAd(AdMobAdvertisings client, AdId id) : base(id) {
                this.client = client;
                this.retry = 0;
                this.placement = "NULL";
                this.receivedReward = false;
                ad = null;

                if (HasAutoLoad(LoadType.OnCreated)) {
                    Load();
                }
            }

            public override Ad Clone() {
                return new AdMobRewardedAd(client, AdId.Clone());
            }

            public override bool Load() {
                if (IsReady) {
                    Log.Warning("[AdMobRewardedAd] Load failed! Rewarded ad is ready!");
                    return false;
                }

                if (IsReloading) {
                    Log.Warning("[AdMobRewardedAd] Load failed! Rewarded ad is reloading!");
                    return false;
                }

                if (IsLoading) {
                    Log.Warning("[AdMobRewardedAd] Load failed! Rewarded ad is loading!");
                    return false;
                }

                if (string.IsNullOrEmpty(Id)) {
                    Log.Warning("[AdMobRewardedAd] Load failed! Rewarded ad id is null or empty!");
                    return false;
                }

                IsLoading = true;

                InvokeOnLoadEvent(placement);

                AdRequest request = new AdRequest();
                GoogleMobileAds.Api.RewardedAd.Load(Id, request, OnAdLoadEvent);
                return true;
            }

            public override bool Show(Action onCompleted, Action onFailed, string placement) {
                if (!IsReady) {
                    Log.Warning("[AdMobRewardedAd] Show failed! Rewarded ad not avaliable!");
                    return false;
                }

                if (IsShowing) {
                    Log.Warning("[AdMobRewardedAd] Show failed! Rewarded ad is showing!");
                    return false;
                }

                IsShowing = true;
                receivedReward = false;
                this.onCompleted = onCompleted;
                this.placement = placement;

                ad.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
                ad.OnAdFullScreenContentFailed += OnAdFullScreenContentFailed;
                ad.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
                ad.OnAdClicked += OnAdClicked;
                ad.OnAdPaid += OnAdPaid;
                ad.OnAdImpressionRecorded += OnAdImpressionRecorded;

                InvokeOnDisplayEvent(placement);

                ad.Show(OnUserRewardEarned);
                return true;
            }

            public override bool Destroy() {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;

                    if (IsShowing) {
                        IsShowing = false;

                        InvokeOnClosedEvent(placement);
                    }

                    if (HasAutoLoad(LoadType.OnDestroyed)) {
                        Reload(true);
                    }

                    return true;
                }

                return false;
            }

            private void OnAdLoadEvent(GoogleMobileAds.Api.RewardedAd ad, LoadAdError error) {
                IsLoading = false;

                if (error == null) {
                    this.ad = ad;
                    InvokeOnLoadedEvent(placement);

                    retry = 0;
                } else {
                    InvokeOnLoadFailedEvent(placement, error.GetMessage());

                    if (HasAutoLoad(LoadType.OnLoadFailed)) {
                        retry++;
                        float delay = 2 * Math.Min(6, retry);
                        IsReloading = true;
                        Invoke(Reload, delay);
                    }
                }
            }

            private void OnAdFullScreenContentOpened() {
                InvokeOnDisplayedEvent(placement);
            }

            private void OnAdFullScreenContentFailed(AdError error) {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;
                }

                receivedReward = false;
                IsShowing = false;
                Invoke(onCompleted);
                InvokeOnDisplayFailedEvent(placement, error.GetMessage());

                if (HasAutoLoad(LoadType.OnDisplayFailed)) {
                    Reload(true);
                }
            }

            private void OnAdClicked() {
                InvokeOnClickedEvent(placement);
            }

            private void OnAdPaid(AdValue value) {
                AdRevenuePaid adRevenuePaid = new AdRevenuePaid() {
                    adSource = client.Network.ToString(),
                    adNetwork = client.Network,
                    adUnitId = Id,
                    adFormat = Format,
                    placement = placement,
                    value = value.Value / 1000000d,
                    currency = value.CurrencyCode,
                };

                InvokeOnRevenuePaidEvent(adRevenuePaid);
            }

            private void OnAdImpressionRecorded() {
            }

            private void OnUserRewardEarned(GoogleMobileAds.Api.Reward reward) {
                receivedReward = true;
                CheckReceiveRewardProcess();
            }
            private void OnAdFullScreenContentClosed() {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;
                }

                IsShowing = false;
                CheckReceiveRewardProcess();

                InvokeOnClosedEvent(placement);

                if (HasAutoLoad(LoadType.OnClosed)) {
                    Reload(true);
                }
            }

            private void CheckReceiveRewardProcess() {
                if (!IsShowing) {
                    if (receivedReward) {
                        Invoke(onCompleted);
                    } else {
                        Invoke(onFailed);
                    }
                    receivedReward = false;
                }
            }
        }

        private class AdMobRewardedAdPreload : Services.Advertisings.RewardedAd {
            private AdMobAdvertisings client;
            private string placement;
            private Action onCompleted;
            private Action onFailed;
            private int retry;
            private bool receivedReward;
            private GoogleMobileAds.Api.RewardedAd ad;

            public override AdService Client => client;
            public override bool IsReady {
                get {
                    return GoogleMobileAds.Api.RewardedAd.IsAdAvailable(Id);
                }
            }

            public AdMobRewardedAdPreload(AdMobAdvertisings client, AdId id) : base(id) {
                this.client = client;
                this.retry = 0;
                this.placement = "NULL";
                this.receivedReward = false;
                ad = null;

                if (HasAutoLoad(LoadType.OnCreated)) {
                    Load();
                }
            }

            public override Ad Clone() {
                return new AdMobRewardedAdPreload(client, AdId.Clone());
            }

            public override bool Load() {
                return false;
            }

            public override bool Show(Action onCompleted, Action onFailed, string placement) {
                if (!IsReady) {
                    Log.Warning("[AdMobRewardedAd] Show failed! Rewarded ad not avaliable!");
                    return false;
                }

                if (IsShowing) {
                    Log.Warning("[AdMobRewardedAd] Show failed! Rewarded ad is showing!");
                    return false;
                }

                IsShowing = true;
                receivedReward = false;
                this.onCompleted = onCompleted;
                this.placement = placement;

                ad = GoogleMobileAds.Api.RewardedAd.PollAd(Id);
                ad.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
                ad.OnAdFullScreenContentFailed += OnAdFullScreenContentFailed;
                ad.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
                ad.OnAdClicked += OnAdClicked;
                ad.OnAdPaid += OnAdPaid;
                ad.OnAdImpressionRecorded += OnAdImpressionRecorded;

                InvokeOnDisplayEvent(placement);

                ad.Show(OnUserRewardEarned);
                return true;
            }

            public override bool Destroy() {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;

                    if (IsShowing) {
                        IsShowing = false;

                        InvokeOnClosedEvent(placement);
                    }

                    if (HasAutoLoad(LoadType.OnDestroyed)) {
                        Reload(true);
                    }

                    return true;
                }

                return false;
            }

            public void InvokeOnLoadedEvent() {
                InvokeOnLoadedEvent(placement);
            }

            private void OnAdLoadEvent(GoogleMobileAds.Api.RewardedAd ad, LoadAdError error) {
                IsLoading = false;

                if (error == null) {
                    this.ad = ad;
                    InvokeOnLoadedEvent(placement);

                    retry = 0;
                } else {
                    InvokeOnLoadFailedEvent(placement, error.GetMessage());

                    if (HasAutoLoad(LoadType.OnLoadFailed)) {
                        retry++;
                        float delay = 2 * Math.Min(6, retry);
                        IsReloading = true;
                        Invoke(Reload, delay);
                    }
                }
            }

            private void OnAdFullScreenContentOpened() {
                InvokeOnDisplayedEvent(placement);
            }

            private void OnAdFullScreenContentFailed(AdError error) {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;
                }

                receivedReward = false;
                IsShowing = false;
                Invoke(onCompleted);
                InvokeOnDisplayFailedEvent(placement, error.GetMessage());

                if (HasAutoLoad(LoadType.OnDisplayFailed)) {
                    Reload(true);
                }
            }

            private void OnAdClicked() {
                InvokeOnClickedEvent(placement);
            }

            private void OnAdPaid(AdValue value) {
                AdRevenuePaid adRevenuePaid = new AdRevenuePaid() {
                    adSource = client.Network.ToString(),
                    adNetwork = client.Network,
                    adUnitId = Id,
                    adFormat = Format,
                    placement = placement,
                    value = value.Value / 1000000d,
                    currency = value.CurrencyCode,
                };

                InvokeOnRevenuePaidEvent(adRevenuePaid);
            }

            private void OnAdImpressionRecorded() {
            }

            private void OnUserRewardEarned(GoogleMobileAds.Api.Reward reward) {
                receivedReward = true;
                CheckReceiveRewardProcess();
            }

            private void OnAdFullScreenContentClosed() {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;
                }

                IsShowing = false;
                CheckReceiveRewardProcess();

                InvokeOnClosedEvent(placement);

                if (HasAutoLoad(LoadType.OnClosed)) {
                    Reload(true);
                }
            }

            private void CheckReceiveRewardProcess() {
                if (!IsShowing) {
                    if (receivedReward) {
                        Invoke(onCompleted);
                    } else {
                        Invoke(onFailed);
                    }
                    receivedReward = false;
                }
            }
        }

        private class AdMobBannerAd : BannerAd {
            private AdMobAdvertisings client;
            private string placement;
            private BannerView bannerView;

            public override AdService Client => client;
            public override bool IsReady {
                get {
                    return true;
                }
            }
            public override float Width {
                get {
                    if (IsShowing) {
                        return bannerView.GetWidthInPixels();
                    }
                    return 0;
                }
            }
            public override float Height {
                get {
                    if (IsShowing) {
                        return bannerView.GetHeightInPixels();
                    }
                    return 0;
                }
            }

            public AdMobBannerAd(AdMobAdvertisings client, AdId id, bool collapsible, AdSizeOption size) : base(id, collapsible, size) {
                this.client = client;
                this.placement = "NULL";
                this.bannerView = null;

                if (HasAutoLoad(LoadType.OnCreated)) {
                    Load();
                }
            }

            public override Ad Clone() {
                AdMobBannerAd ad = new AdMobBannerAd(client, AdId.Clone(), Collapsible, Size);
                return ad;
            }

            public override bool Load() {
                return InternalCreate(Position, Offset);
            }

            private bool InternalCreate(AdPositions position, Vector2Int offset) {

                if (Created) {
                    Log.Warning("[AdMobBannerAd] Banner ad has created!");
                    return false;
                }

                if (string.IsNullOrEmpty(Id)) {
                    Log.Warning("[AdMobBannerAd] Create ad failed! Banner ad id is null or empty!");
                    return false;
                }

                Created = true;
                Position = position;
                Offset = offset;

                bannerView = new BannerView(Id, Size.ToAdSize(), position.ToAdPosition());

                bannerView.OnBannerAdLoaded += OnBannerAdLoaded;
                bannerView.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
                bannerView.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
                bannerView.OnAdClicked += OnAdClicked;
                bannerView.OnAdPaid += OnAdPaid;
                bannerView.OnAdImpressionRecorded += OnAdImpressionRecorded;

                AdRequest request = new AdRequest();
                if (Collapsible) {
                    switch (position) {
                        case AdPositions.TopCenter:
                        case AdPositions.TopLeft:
                        case AdPositions.TopRight:
                            request.Extras.Add("collapsible", "top");
                            break;
                        case AdPositions.Centered:
                        case AdPositions.CenterLeft:
                        case AdPositions.CenterRight:
                            break;
                        case AdPositions.BottomCenter:
                        case AdPositions.BottomLeft:
                        case AdPositions.BottomRight:
                            request.Extras.Add("collapsible", "bottom");
                            break;
                    }
                }
                bannerView.LoadAd(request);

                if (offset == Vector2Int.zero) {
                    bannerView.SetPosition(position.ToAdPosition());
                } else {
                    Vector2Int adScreenPosition = bannerView.GetAdScreenPosition(position, offset);
                    bannerView.SetPosition(adScreenPosition.x, adScreenPosition.y);
                }

                bannerView.Hide();

                return true;
            }

            public override bool Show(AdPositions position, Vector2Int offset, string placement) {
                if (!Created) {
                    Log.Warning("[AdMobBannerAd] Banner ad not been created! Create a new banner ad.");
                    if (!InternalCreate(position, offset)) {
                        return false;
                    }
                }

                if (Position != position || Offset != offset) {
                    if (Collapsible) {
                        Destroy();
                        InternalCreate(position, offset);
                    }

                    if (offset == Vector2Int.zero) {
                        bannerView.SetPosition(position.ToAdPosition());
                    } else {
                        Vector2Int adScreenPosition = bannerView.GetAdScreenPosition(position, offset);
                        bannerView.SetPosition(adScreenPosition.x, adScreenPosition.y);
                    }
                }

                IsShowing = true;
                IsLoading = true;
                this.Position = position;
                this.Offset = offset;
                this.placement = placement;

                InvokeOnDisplayEvent( placement);

                bannerView.Show();

                InvokeOnDisplayedEvent(placement);

                return true;
            }

            public override void SetPosition(AdPositions position, Vector2Int offset) {
                if (position != Position || offset != Offset) {
                    this.Position = position;
                    this.Offset = offset;

                    if (IsReady) {
                        if (Offset == Vector2Int.zero) {
                            bannerView.SetPosition(Position.ToAdPosition());
                        } else {
                            Vector2Int adScreenPosition = bannerView.GetAdScreenPosition(Position, Offset);
                            bannerView.SetPosition(adScreenPosition.x, adScreenPosition.y);
                        }
                    }
                }
            }

            public override bool Hide() {
                if (!IsShowing) {
                    Log.Warning("[AdMobBannerAd] Banner ad not showing!");
                    return false;
                }

                IsShowing = false;
                bannerView.Hide();

                InvokeOnClosedEvent(placement);

                return true;
            }

            public override bool Destroy() {
                if (!Created) {
                    Log.Warning("[AdMobBannerAd] Banner ad not been created!");
                    return false;
                }

                Created = false;

                if (IsShowing) {
                    IsShowing = false;

                    InvokeOnClosedEvent(placement);
                }

                bannerView.OnBannerAdLoaded -= OnBannerAdLoaded;
                bannerView.OnAdFullScreenContentOpened -= OnAdFullScreenContentOpened;
                bannerView.OnAdFullScreenContentClosed -= OnAdFullScreenContentClosed;
                bannerView.OnAdClicked -= OnAdClicked;
                bannerView.OnAdPaid -= OnAdPaid;
                bannerView.OnAdImpressionRecorded -= OnAdImpressionRecorded;
                bannerView.Destroy();
                bannerView = null;
                this.Position = AdPositions.Null;
                this.Offset = Vector2Int.zero;
                return true;
            }

            private void OnBannerAdLoaded() {
                IsLoading = false;

                if (Offset == Vector2Int.zero) {
                    bannerView.SetPosition(Position.ToAdPosition());
                } else {
                    Vector2Int adScreenPosition = bannerView.GetAdScreenPosition(Position, Offset);
                    bannerView.SetPosition(adScreenPosition.x, adScreenPosition.y);
                }

                InvokeOnLoadedEvent(placement);
            }

            private void OnAdFullScreenContentOpened() {
            }

            private void OnAdClicked() {
                InvokeOnClickedEvent(placement);
            }

            private void OnAdPaid(AdValue value) {
                AdRevenuePaid adRevenuePaid = new AdRevenuePaid() {
                    adSource = client.Network.ToString(),
                    adNetwork = client.Network,
                    adUnitId = Id,
                    adFormat = Format,
                    placement = placement,
                    value = value.Value / 1000000d,
                    currency = value.CurrencyCode,
                };

                InvokeOnRevenuePaidEvent(adRevenuePaid);
            }

            private void OnAdImpressionRecorded() {
            }

            private void OnAdFullScreenContentClosed() {
            }
        }

        private class AdMobMediumRectangleAd : MediumRectangleAd {
            private AdMobAdvertisings client;
            private string placement;
            private BannerView bannerView;
            public override AdService Client => client;
            public override bool IsReady {
                get {
                    return true;
                }
            }
            public override float Width {
                get {
                    if (IsShowing) {
                        return bannerView.GetWidthInPixels();
                    }
                    return 0;
                }
            }
            public override float Height {
                get {
                    if (IsShowing) {
                        return bannerView.GetHeightInPixels();
                    }
                    return 0;
                }
            }

            public AdMobMediumRectangleAd(AdMobAdvertisings client, AdId id) : base(id) {
                this.client = client;
                this.placement = "NULL";
                this.bannerView = null;

                if (HasAutoLoad(LoadType.OnCreated)) {
                    Load();
                }
            }

            public override bool Load() {
                if (Created) {
                    Log.Warning("[AdMobMediumRectangleAd] Medium rectangle ad has created!");
                    return false;
                }

                if (string.IsNullOrEmpty(Id)) {
                    Log.Warning("[AdMobMediumRectangleAd] Create ad failed! Medium rectangle ad id is null or empty!");
                    return false;
                }

                Created = true;

                bannerView = new BannerView(Id, AdSize.MediumRectangle, AdPositions.BottomCenter.ToAdPosition());

                bannerView.OnBannerAdLoaded += OnBannerAdLoaded;
                bannerView.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
                bannerView.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
                bannerView.OnAdClicked += OnAdClicked;
                bannerView.OnAdPaid += OnAdPaid;
                bannerView.OnAdImpressionRecorded += OnAdImpressionRecorded;

                AdRequest request = new AdRequest();
                bannerView.LoadAd(request);

                bannerView.Hide();

                return true;
            }

            public override Ad Clone() {
                return new AdMobMediumRectangleAd(client, AdId.Clone());
            }

            public override bool Show(AdPositions position, Vector2Int offset, string placement) {
                if (!Created) {
                    Log.Warning("[AdMobMediumRectangleAd] Medium rectangle ad not been created! Create a new medium rectangle.");
                    if (!Load()) {
                        return false;
                    }
                }

                if (Position != position || Offset != offset) {

                    if (offset == Vector2Int.zero) {
                        bannerView.SetPosition(position.ToAdPosition());
                    } else {
                        Vector2Int adScreenPosition = AdMobExtensions.GetAdScreenPositionInDp(300, 250, position, offset);
                        bannerView.SetPosition(adScreenPosition.x, adScreenPosition.y);
                    }
                }

                IsShowing = true;
                IsLoading = true;
                this.Position = position;
                this.Offset = offset;
                this.placement = placement;

                InvokeOnDisplayEvent(placement);

                bannerView.Show();

                InvokeOnDisplayedEvent(placement);

                return true;
            }

            public override void SetPosition(AdPositions position, Vector2Int offset) {
                if (position != Position || offset != Offset) {
                    this.Position = position;
                    this.Offset = offset;

                    if (IsReady) {
                        if (offset == Vector2Int.zero) {
                            bannerView.SetPosition(position.ToAdPosition());
                        } else {
                            Vector2Int adScreenPosition = AdMobExtensions.GetAdScreenPositionInDp(300, 250, position, offset);
                            bannerView.SetPosition(adScreenPosition.x, adScreenPosition.y);
                        }
                    }
                }
            }

            public override bool Hide() {
                if (!IsShowing) {
                    Log.Warning("[AdMobMediumRectangleAd] Medium rectangle ad not showing!");
                    return false;
                }

                IsShowing = false;
                bannerView.Hide();

                InvokeOnClosedEvent(placement);

                return true;
            }

            public override bool Destroy() {
                if (!Created) {
                    Log.Warning("[AdMobMediumRectangleAd] Medium rectangle ad not been created!");
                    return false;
                }

                Created = false;

                if (IsShowing) {
                    IsShowing = false;

                    InvokeOnClosedEvent(placement);
                }

                bannerView.OnBannerAdLoaded -= OnBannerAdLoaded;
                bannerView.OnAdFullScreenContentOpened -= OnAdFullScreenContentOpened;
                bannerView.OnAdFullScreenContentClosed -= OnAdFullScreenContentClosed;
                bannerView.OnAdClicked -= OnAdClicked;
                bannerView.OnAdPaid -= OnAdPaid;
                bannerView.OnAdImpressionRecorded -= OnAdImpressionRecorded;
                bannerView.Destroy();
                bannerView = null;
                this.Position = AdPositions.Null;
                this.Offset = Vector2Int.zero;
                return true;
            }

            private void OnBannerAdLoaded() {
                IsLoading = false;
                InvokeOnLoadedEvent(placement);
            }

            private void OnAdFullScreenContentOpened() {
            }

            private void OnAdClicked() {
                InvokeOnClickedEvent(placement);
            }

            private void OnAdPaid(AdValue value) {
                AdRevenuePaid adRevenuePaid = new AdRevenuePaid() {
                    adSource = client.Network.ToString(),
                    adNetwork = client.Network,
                    adUnitId = Id,
                    adFormat = Format,
                    placement = placement,
                    value = value.Value / 1000000d,
                    currency = value.CurrencyCode,
                };

                InvokeOnRevenuePaidEvent(adRevenuePaid);
            }

            private void OnAdImpressionRecorded() {
            }

            private void OnAdFullScreenContentClosed() {
            }
        }


#if ADMOB_NATIVE
        private class AdMobNativeAd : Services.Advertisings.NativeAd {
            private AdMobAdvertisings client;
            private NativeAdView view;
            private string placement;
            private int retry;
            private GoogleMobileAds.Api.NativeAd ad;

            public override AdService Client => client;
            public override bool IsReady {
                get {
                    return ad != null;
                }
            }

            public AdMobNativeAd(AdMobAdvertisings client, AdId id) : base(id) {
                this.client = client;
                this.retry = 0;
                this.placement = "NULL";
                ad = null;

                AddElemnet(new StringElement(ElementType.Name, GetHeadlineText, RegisterHeadlineText));
                AddElemnet(new TextureElement(ElementType.Image, GetImageTexture, RegisterImageTexture));
                AddElemnet(new StringElement(ElementType.Description, GetBodyText, RegisterBodyText));
                AddElemnet(new TextureElement(ElementType.Icon, GetIconTexture, RegisterIconTexture));
                AddElemnet(new TextureElement(ElementType.AdChoicesLogo, GetAdChoicesLogoTexture, RegisterAdChoicesLogoTexture));
                AddElemnet(new StringElement(ElementType.CTA, GetCallToActionText, RegisterCallToActionText));
                AddElemnet(new DoubleElement(ElementType.StarRating, GetStarRating, RegisterStarRating));
                AddElemnet(new StringElement(ElementType.Advertiser, GetAdvertiserText, RegisterAdvertiserText));
                AddElemnet(new StringElement(ElementType.Price, GetPriceText, RegisterPriceText));

                if (HasAutoLoad(LoadType.OnCreated)) {
                    Load();
                }
            }
            
            public override Ad Clone() {
                return new AdMobNativeAd(client, AdId.Clone());
            }

            public override bool Load() {
                if (IsReady) {
                    Log.Warning("[AdMobNativeAd] Load failed! Native ad is ready!");
                    return false;
                }

                if (IsReloading) {
                    Log.Warning("[AdMobNativeAd] Load failed! Native ad is reloading!");
                    return false;
                }

                if (IsLoading) {
                    Log.Warning("[AdMobNativeAd] Load failed! Native ad is loading!");
                    return false;
                }

                if (string.IsNullOrEmpty(Id)) {
                    Log.Warning("[AdMobNativeAd] Load failed! Native ad id is null or empty!");
                    return false;
                }


                IsLoading = true;

                InvokeOnLoadEvent(placement);

                AdLoader adLoader = new AdLoader.Builder(Id).ForNativeAd().Build();
                AdRequest request = new AdRequest();

                adLoader.OnNativeAdLoaded += OnNativeAdLoaded;
                adLoader.OnAdLoadFailed += OnAdFailedToLoad;
                adLoader.OnNativeAdOpening += OnNativeAdOpening;
                adLoader.OnNativeAdClicked += OnNativeAdClicked;
                adLoader.OnNativeAdClosed += OnNativeAdClosed;
                adLoader.OnNativeAdImpression += OnNativeAdImpression;

                adLoader.LoadAd(request);

                return true;
            }

            public override bool Show(NativeAdView view, string placement) {
                if (!IsReady) {
                    Log.Warning("[AdMobNativeAd] Show failed! Native ad not avaliable!");
                    return false;
                }

                if (IsShowing) {
                    Log.Warning("[AdMobNativeAd] Show failed! Native ad is showing!");
                    return false;
                }

                IsShowing = true;
                this.placement = placement;
                this.view = view;

                this.view.onNativeAdDisplayed += OnNativeAdDisplayed;
                this.view.onNativeAdDisplayFailed += OnNativeAdDisplayFailed;

                InvokeOnDisplayEvent(placement);

                this.view.Show(this);

                return true;
            }

            public override bool Hide() {
                if (!IsShowing) {
                    Log.Warning("[AdMobNativeAd] Native ad not showing!");
                    return false;
                }

                IsShowing = false;
                if (view != null) {
                    view.Hide();
                    view = null;
                }

                InvokeOnClosedEvent(placement);

                if (HasAutoLoad(LoadType.OnClosed)) {
                    Reload(true);
                }

                return true;
            }


            public override bool Destroy() {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;
                }

                if (view != null) {
                    view.Destroy();
                    view = null;

                    if (IsShowing) {
                        IsShowing = false;

                        InvokeOnClosedEvent(placement);
                    }

                    if (HasAutoLoad(LoadType.OnDestroyed)) {
                        Reload(true);
                    }

                    return true;
                }

                return false;
            }


            #region Elements
            private string GetHeadlineText() {
                if (ad != null) {
                    return ad.GetHeadlineText();
                }
                return null;
            }
            private bool RegisterHeadlineText(GameObject gameObject) {
                if (ad != null) {
                    return ad.RegisterHeadlineTextGameObject(gameObject);
                }
                return false;
            }
            private Texture2D GetImageTexture() {
                if (ad != null) {
                    List<Texture2D> result = ad.GetImageTextures();
                    if (result != null && result.Count > 0) {
                        return result[0];
                    }
                }
                return null;
            }
            private bool RegisterImageTexture(GameObject gameObject) {
                if (ad != null) {
                    return ad.RegisterImageGameObjects(new List<GameObject>() { gameObject }) > 0;
                }
                return false;
            }
            private string GetBodyText() {
                if (ad != null) {
                    return ad.GetBodyText();
                }
                return null;
            }
            private bool RegisterBodyText(GameObject gameObject) {
                if (ad != null) {
                    return ad.RegisterBodyTextGameObject(gameObject);
                }
                return false;
            }
            private Texture2D GetIconTexture() {
                if (ad != null) {
                    return ad.GetIconTexture();
                }
                return null;
            }
            private bool RegisterIconTexture(GameObject gameObject) {
                if (ad != null) {
                    return ad.RegisterIconImageGameObject(gameObject);
                }
                return false;
            }
            private Texture2D GetAdChoicesLogoTexture() {
                if (ad != null) {
                    return ad.GetAdChoicesLogoTexture();
                }
                return null;
            }
            private bool RegisterAdChoicesLogoTexture(GameObject gameObject) {
                if (ad != null) {
                    return ad.RegisterAdChoicesLogoGameObject(gameObject);
                }
                return false;
            }
            private string GetCallToActionText() {
                if (ad != null) {
                    return ad.GetCallToActionText();
                }
                return null;
            }
            private bool RegisterCallToActionText(GameObject gameObject) {
                if (ad != null) {
                    return ad.RegisterCallToActionGameObject(gameObject);
                }
                return false;
            }
            private double GetStarRating() {
                if (ad != null) {
                    return ad.GetStarRating();
                }
                return 4.5d;
            }
            private bool RegisterStarRating(GameObject gameObject) {
                return true;
            }
            private string GetAdvertiserText() {
                if (ad != null) {
                    return ad.GetAdvertiserText();
                }
                return null;
            }
            private bool RegisterAdvertiserText(GameObject gameObject) {
                if (ad != null) {
                    return ad.RegisterAdvertiserTextGameObject(gameObject);
                }
                return false;
            }
            private string GetPriceText() {
                if (ad != null) {
                    return ad.GetPrice();
                }
                return null;
            }
            private bool RegisterPriceText(GameObject gameObject) {
                if (ad != null) {
                    return ad.RegisterPriceGameObject(gameObject);
                }
                return false;
            }
            #endregion

            private void OnNativeAdLoaded(object sender, NativeAdEventArgs e) {
                IsLoading = false;

                if (e.nativeAd == null) {
                    InvokeOnLoadFailedEvent(placement, "Unexpected error: Native ad load event fired with null ad.");

                    if (HasAutoLoad(LoadType.OnLoadFailed)) {
                        retry++;
                        float delay = 2 * Math.Min(6, retry);
                        IsReloading = true;
                        Invoke(Reload, delay);
                    }
                } else {
                    this.ad = e.nativeAd;
                    this.ad.OnAdPaid += OnPaidEvent;

                    InvokeOnLoadedEvent(placement);

                    retry = 0;
                }
            }

            private void OnAdFailedToLoad(LoadAdError e) {
                IsLoading = false;

                InvokeOnLoadFailedEvent(placement, e.GetMessage());

                if (HasAutoLoad(LoadType.OnLoadFailed)) {
                    retry++;
                    float delay = 2 * Math.Min(6, retry);
                    IsReloading = true;
                    Invoke(Reload, delay);
                }
            }

            private void OnNativeAdDisplayed() {
                InvokeOnDisplayedEvent(placement);
            }

            private void OnNativeAdDisplayFailed(string error) {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;
                }

                IsShowing = false;

                InvokeOnDisplayFailedEvent(placement, error);

                if (HasAutoLoad(LoadType.OnDisplayFailed)) {
                    Reload(true);
                }
            }

            private void OnNativeAdOpening(object sender, System.EventArgs e) {
                InvokeOnDisplayedEvent(placement);
            }

            private void OnNativeAdClicked(object sender, System.EventArgs e) {
                InvokeOnClickedEvent(placement);
            }

            private void OnNativeAdClosed(object sender, System.EventArgs e) {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;
                }

                IsShowing = false;

                InvokeOnClosedEvent(placement);

                if (HasAutoLoad(LoadType.OnClosed)) {
                    Reload(true);
                }
            }

            private void OnNativeAdImpression(object sender, System.EventArgs e) {

            }

            private void OnPaidEvent(AdValue e) {
                AdRevenuePaid adRevenuePaid = new AdRevenuePaid() {
                    adSource = client.Network.ToString(),
                    adNetwork = client.Network,
                    adUnitId = Id,
                    adFormat = Format,
                    placement = placement,
                    value = e.Value / 1000000d,
                    currency = e.CurrencyCode,
                };

                InvokeOnRevenuePaidEvent(adRevenuePaid);
            }
        }
#endif

        private class AdMobRewardedInterstitialAd : Services.Advertisings.RewardedInterstitialAd {
            private AdMobAdvertisings client;
            private string placement;
            private Action onCompleted;
            private Action onFailed;
            private int retry;
            private bool receivedReward;
            private GoogleMobileAds.Api.RewardedInterstitialAd ad;

            public override AdService Client => client;
            public override bool IsReady {
                get {
                    if (ad != null) {
                        bool canShow = ad.CanShowAd();

                        if (canShow) {
                            return true;
                        } else {
                            Log.Warning("[AdMobRewardedAd] Rewarded ad can only be shown once per load!");
                            ad.Destroy();
                            ad = null;
                            if (HasAutoLoad(LoadType.OnExpried)) {
                                Reload(true);
                            }
                            return false;
                        }
                    }
                    return false;
                }
            }

            public AdMobRewardedInterstitialAd(AdMobAdvertisings client, AdId id) : base(id) {
                this.client = client;
                this.retry = 0;
                this.placement = "NULL";
                this.receivedReward = false;
                ad = null;

                if (HasAutoLoad(LoadType.OnCreated)) {
                    Load();
                }
            }


            public override Ad Clone() {
                return new AdMobRewardedInterstitialAd(client, AdId.Clone());
            }

            public override bool Load() {
                if (IsReady) {
                    Log.Warning("[AdMobRewardedInterstitialAd] Load failed! Rewarded interstitial ad is ready!");
                    return false;
                }

                if (IsReloading) {
                    Log.Warning("[AdMobRewardedInterstitialAd] Load failed! Rewarded interstitial ad is reloading!");
                    return false;
                }

                if (IsLoading) {
                    Log.Warning("[AdMobRewardedInterstitialAd] Load failed! Rewarded interstitial ad is loading!");
                    return false;
                }

                if (string.IsNullOrEmpty(Id)) {
                    Log.Warning("[AdMobRewardedInterstitialAd] Load failed! Rewarded interstitial ad id is null or empty!");
                    return false;
                }

                IsLoading = true;

                InvokeOnLoadEvent(placement);

                AdRequest request = new AdRequest();
                GoogleMobileAds.Api.RewardedInterstitialAd.Load(Id, request, OnAdLoadEvent);
                return true;
            }

            public override bool Show(Action onCompleted, Action onFailed, string placement) {
                if (!IsReady) {
                    Log.Warning("[AdMobRewardedInterstitialAd] Show failed! Rewarded interstitial ad not avaliable!");
                    return false;
                }

                if (IsShowing) {
                    Log.Warning("[AdMobRewardedInterstitialAd] Show failed! Rewarded interstitial ad is showing!");
                    return false;
                }

                IsShowing = true;
                receivedReward = false;
                this.onCompleted = onCompleted;
                this.placement = placement;

                ad.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
                ad.OnAdFullScreenContentFailed += OnAdFullScreenContentFailed;
                ad.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
                ad.OnAdClicked += OnAdClicked;
                ad.OnAdPaid += OnAdPaid;
                ad.OnAdImpressionRecorded += OnAdImpressionRecorded;

                InvokeOnDisplayEvent(placement);

                ad.Show(OnUserRewardEarned);
                return true;
            }

            public override bool Destroy() {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;

                    if (IsShowing) {
                        IsShowing = false;

                        InvokeOnClosedEvent(placement);
                    }

                    if (HasAutoLoad(LoadType.OnDestroyed)) {
                        Reload(true);
                    }

                    return true;
                }

                return false;
            }

            private void OnAdLoadEvent(GoogleMobileAds.Api.RewardedInterstitialAd ad, LoadAdError error) {
                IsLoading = false;

                if (error == null) {
                    this.ad = ad;
                    InvokeOnLoadedEvent(placement);

                    retry = 0;
                } else {
                    InvokeOnLoadFailedEvent(placement, error.GetMessage());

                    if (HasAutoLoad(LoadType.OnLoadFailed)) {
                        retry++;
                        float delay = 2 * Math.Min(6, retry);
                        IsReloading = true;
                        Invoke(Reload, delay);
                    }
                }
            }

            private void OnAdFullScreenContentOpened() {
                InvokeOnDisplayedEvent(placement);
            }

            private void OnAdFullScreenContentFailed(AdError error) {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;
                }

                receivedReward = false;
                IsShowing = false;
                Invoke(onFailed);
                InvokeOnDisplayFailedEvent(placement, error.GetMessage());

                if (HasAutoLoad(LoadType.OnDisplayFailed)) {
                    Reload(true);
                }
            }

            private void OnAdClicked() {
                InvokeOnClickedEvent(placement);
            }

            private void OnAdPaid(AdValue value) {
                AdRevenuePaid adRevenuePaid = new AdRevenuePaid() {
                    adSource = client.Network.ToString(),
                    adNetwork = client.Network,
                    adUnitId = Id,
                    adFormat = Format,
                    placement = placement,
                    value = value.Value / 1000000d,
                    currency = value.CurrencyCode,
                };

                InvokeOnRevenuePaidEvent(adRevenuePaid);
            }

            private void OnAdImpressionRecorded() {
            }

            private void OnUserRewardEarned(GoogleMobileAds.Api.Reward reward) {
                receivedReward = true;
                CheckReceiveRewardProcess();
            }

            private void OnAdFullScreenContentClosed() {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;
                }

                IsShowing = false;
                CheckReceiveRewardProcess();

                InvokeOnClosedEvent(placement);

                if (HasAutoLoad(LoadType.OnClosed)) {
                    Reload(true);
                }
            }

            private void CheckReceiveRewardProcess() {
                if (!IsShowing) {
                    if (receivedReward) {
                        Invoke(onCompleted);
                    } else {
                        Invoke(onFailed);
                    }
                    receivedReward = false;
                }
            }
        }

        private class AdMobNativeOverlayAd : Services.Advertisings.NativeOverlayAd {
            private AdMobAdvertisings client;
            private string placement;
            private int retry;
            private GoogleMobileAds.Api.NativeOverlayAd ad;

            public override AdService Client => client;
            public override bool IsReady {
                get {
                    return ad != null;
                }
            }
            public override float Width {
                get {
                    if (IsShowing) {
                        return ad.GetTemplateWidthInPixels();
                    }
                    return 0;
                }
            }
            public override float Height {
                get {
                    if (IsShowing) {
                        return ad.GetTemplateHeightInPixels();
                    }
                    return 0;
                }
            }

            public AdMobNativeOverlayAd(AdMobAdvertisings client, AdId id) : base(id) {
                this.client = client;
                this.placement = "NULL";
                this.ad = null;

                if (HasAutoLoad(LoadType.OnCreated)) {
                    Load();
                }
            }

            public override Ad Clone() {
                return new AdMobNativeOverlayAd(client, AdId.Clone());
            }

            public override bool Load() {
                if (IsReady) {
                    Log.Warning("[AdMobNativeOverlayAd] Load failed! Native overlay ad is ready!");
                    return false;
                }

                if (IsReloading) {
                    Log.Warning("[AdMobNativeOverlayAd] Load failed! Native overlay ad is reloading!");
                    return false;
                }

                if (IsLoading) {
                    Log.Warning("[AdMobNativeOverlayAd] Load failed! Native overlay ad is loading!");
                    return false;
                }

                if (string.IsNullOrEmpty(Id)) {
                    Log.Warning("[AdMobNativeOverlayAd] Load failed! Native overlay ad id is null or empty!");
                    return false;
                }

                IsLoading = true;
                InvokeOnLoadEvent(placement);

                AdRequest request = new AdRequest();

                NativeAdOptions options = new NativeAdOptions {
                    MediaAspectRatio = MediaAspectRatio.Any,
                    AdChoicesPlacement = AdChoicesPlacement.TopRightCorner,
                };

                GoogleMobileAds.Api.NativeOverlayAd.Load(Id, request, options, OnAdLoadEvent);
                return true;
            }

            public override bool Show(AdPositions position, Vector2Int offset, NativeOverlayAdStyle style, AdSizeOption size, string placement) {
                if (!IsReady) {
                    Log.Warning("[AdMobNativeOverlayAd] Show failed! Native overlay ad not avaliable!");
                    return false;
                }

                if (IsShowing) {
                    Log.Warning("[AdMobNativeOverlayAd] Show failed! Native overlay ad is showing!");
                    return false;
                }

                IsShowing = true;

                this.Position = position;
                this.Offset = offset;
                this.Style = style;
                this.Size = size;
                this.placement = placement;

                ad.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
                ad.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
                ad.OnAdPaid += OnAdPaid;
                ad.OnAdClicked += OnAdClicked;
                ad.OnAdImpressionRecorded += OnAdImpressionRecorded;

                InvokeOnDisplayEvent(placement);

                if (offset == Vector2Int.zero) {
                    ad.RenderTemplate(style.ToAdmobNativeTemplateStyle(), size.ToAdSize(), position.ToAdPosition());
                } else {
                    Vector2Int adScreenPosition = ad.GetAdScreenPosition(position, offset, Size);
                    ad.RenderTemplate(style.ToAdmobNativeTemplateStyle(), size.ToAdSize(), adScreenPosition.x, adScreenPosition.y);
                }

                ad.Show();

                InvokeOnDisplayedEvent(placement);

                return true;
            }

            public override void SetPosition(AdPositions position, Vector2Int offset) {
                if (position != Position || offset != Offset) {
                    this.Position = position;
                    this.Offset = offset;

                    if (IsReady) {
                        if (offset == Vector2Int.zero) {
                            ad.SetTemplatePosition(position.ToAdPosition());
                        } else {
                            Vector2Int adScreenPosition = ad.GetAdScreenPosition(position, offset, Size);
                            ad.SetTemplatePosition(adScreenPosition.x, adScreenPosition.y);
                        }
                    }
                }
            }

            public override bool Hide() {
                if (!IsShowing) {
                    Log.Warning("[AdMobNativeOverlayAd] Native overlay ad not showing!");
                    return false;
                }

                IsShowing = false;
                ad.Hide();

                InvokeOnClosedEvent(placement);

                if (HasAutoLoad(LoadType.OnClosed)) {
                    Reload(true);
                }

                return true;
            }

            public override bool Destroy() {
                if (ad != null) {
                    ad.Destroy();
                    ad = null;

                    if (IsShowing) {
                        IsShowing = false;

                        InvokeOnClosedEvent(placement);
                    }

                    if (HasAutoLoad(LoadType.OnDestroyed)) {
                        Reload(true);
                    }

                    return true;
                }

                return false;
            }

            private void OnAdLoadEvent(GoogleMobileAds.Api.NativeOverlayAd ad, LoadAdError error) {
                IsLoading = false;

                if (error == null) {

                    if (ad == null) {
                        InvokeOnLoadFailedEvent(placement, "Unexpected error: Native overlay ad load event fired with null ad and null error.");

                        if (HasAutoLoad(LoadType.OnLoadFailed)) {
                            retry++;
                            float delay = 2 * Math.Min(6, retry);
                            IsReloading = true;
                            Invoke(Reload, delay);
                        }
                    } else {
                        this.ad = ad;
                        InvokeOnLoadedEvent(placement);
                    }

                    retry = 0;
                } else {
                    InvokeOnLoadFailedEvent(placement, error.GetMessage());

                    if (HasAutoLoad(LoadType.OnLoadFailed)) {
                        retry++;
                        float delay = 2 * Math.Min(6, retry);
                        IsReloading = true;
                        Invoke(Reload, delay);
                    }
                }
            }

            private void OnAdFullScreenContentOpened() {
            }

            private void OnAdClicked() {
                InvokeOnClickedEvent(placement);
            }

            private void OnAdPaid(AdValue value) {
                AdRevenuePaid adRevenuePaid = new AdRevenuePaid() {
                    adSource = client.Network.ToString(),
                    adNetwork = client.Network,
                    adUnitId = Id,
                    adFormat = Format,
                    placement = placement,
                    value = value.Value / 1000000d,
                    currency = value.CurrencyCode,
                };

                InvokeOnRevenuePaidEvent(adRevenuePaid);
            }

            private void OnAdImpressionRecorded() {
            }

            private void OnAdFullScreenContentClosed() {
            }
        }

#if ADMOB_NATIVE_IMMERSIVE
        private class AdMobNativeImmersiveAd : NativeImmersiveAd {
            private AdMobAdvertisings client;
            private string placement;
            private int retry;
            private GoogleMobileAds.Api.ImmersiveInGameDisplayAd ad;

            public override AdService Client => client;
            public override bool IsReady {
                get {
                    return ad != null;
                }
            }

            public AdMobNativeImmersiveAd(AdMobAdvertisings client, AdId id) : base(id) {
                this.client = client;
                this.retry = 0;
                this.placement = "NULL";
                this.ad = null;
                this.aspectRatios = new AdAspectRatio[] { new AdAspectRatio(AdAspectRatioType.AR_1x1) };
                this.canvasMode = false;

                if (HasAutoLoad(LoadType.OnCreated)) {
                    Load();
                }
            }

            public override Ad Clone() {
                return new AdMobNativeImmersiveAd(client, AdId.Clone());
            }

            public override bool Load() {
                if (IsReady) {
                    Log.Warning("[AdMobNativeImmersiveAd] Load failed! Native immersive ad is ready!");
                    return false;
                }

                if (IsReloading) {
                    Log.Warning("[AdMobNativeImmersiveAd] Load failed! Native immersive ad is reloading!");
                    return false;
                }

                if (IsLoading) {
                    Log.Warning("[AdMobNativeImmersiveAd] Load failed! Native immersive ad is loading!");
                    return false;
                }

                if (string.IsNullOrEmpty(Id)) {
                    Log.Warning("[AdMobNativeImmersiveAd] Load failed! Native immersive ad id is null or empty!");
                    return false;
                }

                IsLoading = true;

                InvokeOnLoadEvent(placement);

                AdLoader.Builder builder = new AdLoader.Builder(Id);
                if (aspectRatios.Length > 1) {
                    List<ImmersiveInGameDisplayAdAspectRatio> adSizes = new List<ImmersiveInGameDisplayAdAspectRatio>(AspectRatios.Length);

                    for (int i = 0; i < AspectRatios.Length; i++) {
                        adSizes.Add(AspectRatios[i].ToAdAspectRation());
                    }
                    builder.SetImmersiveInGameDisplayAdAspectRatios(adSizes);
                } else {
                    ImmersiveInGameDisplayAdAspectRatio adSize = AspectRatios[0].ToAdAspectRation();
                    builder.SetImmersiveInGameDisplayAdAspectRatio(adSize);
                }
                if (canvasMode) {
                    builder.ForImmersiveInGameDisplayAd(ImmersiveInGameAdType.DisplayCanvas);
                } else {
                    builder.ForImmersiveInGameDisplayAd(ImmersiveInGameAdType.Display3D);
                }
                if (!clickable) builder.DisableImmersiveClicks();
                if (!adBadgeEnable) builder.DisableAdBadge();
                if (friendlyObjects != null) builder.SetImmersiveClickFriendlyUIGameObjects(friendlyObjects);

                AdLoader adLoader = builder.Build();
                    
                AdRequest request = new AdRequest();

                adLoader.OnImmersiveInGameDisplayAdLoaded += OnNativeAdLoaded;
                adLoader.OnAdLoadFailed += OnAdLoadFailed;
                adLoader.OnImmersiveInGameDisplayAdClicked += OnNativeAdClicked;
                adLoader.OnImmersiveInGameDisplayAdPaidEvent += OnPaidEvent;
                adLoader.OnImmersiveInGameDisplayAdImpression += OnNativeAdImpression;

                adLoader.LoadAd(request);

                return true;
            }

            public override bool Show(string placement) {
                if (!IsReady) {
                    Log.Warning("[AdMobNativeImmersiveAd] Show failed! Native immersive ad not avaliable!");
                    return false;
                }

                if (IsShowing) {
                    Log.Warning("[AdMobNativeImmersiveAd] Show failed! Native immersive ad is showing!");
                    return false;
                }

                IsShowing = true;
                this.placement = placement;

                InvokeOnDisplayEvent(placement);

                this.ad.ShowAd();

                InvokeOnDisplayedEvent(placement);

                return true;
            }

            public override bool Hide() {
                if (!IsReady) {
                    Log.Warning("[AdMobNativeImmersiveAd] Show failed! Native immersive ad not avaliable!");
                    return false;
                }

                if (!IsShowing) {
                    Log.Warning("[AdMobNativeImmersiveAd] Native immersive ad not showing!");
                    return false;
                }

                IsShowing = false;
                InvokeOnClosedEvent(placement);

                if (HasAutoLoad(LoadType.OnClosed)) {
                    Reload(true);
                }

                return true;
            }

            public override bool Destroy() {
                if (!IsReady) {
                    Log.Warning("[AdMobNativeImmersiveAd] Show failed! Native immersive ad not avaliable!");
                    return false;
                }

                if (IsShowing) {
                    IsShowing = false;

                    InvokeOnClosedEvent(placement);
                }

                this.ad.Destroy();
                this.ad = null;

                if (HasAutoLoad(LoadType.OnDestroyed)) {
                    Reload(true);
                }

                return true;
            }

            public override void SetPosition(Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale) {
                if (!IsReady) {
                    Log.Warning("[AdMobNativeImmersiveAd] Set position failed! Native immersive ad not avaliable!");
                    return;
                }

                ad.SetParent(parent.gameObject);
                ad.SetLocalPosition(localPosition);
                ad.SetLocalRotation(localRotation);
                ad.SetLocalScale(localScale.x);
            }

            public override void SetPosition(Vector2 anchorMin, Vector2 anchorMax, Vector2 anchorPosition, Vector2 pivot) {
                if (!IsReady) {
                    Log.Warning("[AdMobNativeImmersiveAd] Set position failed! Native immersive ad not avaliable!");
                    return;
                }

                ad.SetAnchorMin(anchorMin);
                ad.SetAnchorMax(anchorMax);
                ad.SetAnchoredPosition(anchorPosition);
                ad.SetPivot(pivot);
            }

            public override void SetMaterial(Material material) {
                if (!IsReady) {
                    Log.Warning("[AdMobNativeImmersiveAd] Set material failed! Native immersive ad not avaliable!");
                    return;
                }

                ad.SetMaterial(material);
            }

            public override void SetCamera(Camera camera) {
                GoogleMobileAds.Api.ImmersiveInGameDisplayAd.SetCamera(camera);
            }

            private void OnNativeAdLoaded(object sender, ImmersiveInGameDisplayAdEventArgs e) {
                IsLoading = false;

                if (e.ImmersiveInGameDisplayAd == null) {
                    InvokeOnLoadFailedEvent(placement, "Unexpected error: Native overlay ad load event fired with null ad.");

                    if (HasAutoLoad(LoadType.OnLoadFailed)) {
                        retry++;
                        float delay = 2 * Math.Min(6, retry);
                        IsLoading = true;
                        Invoke(Reload, delay);
                    }
                } else {
                    retry = 0;
                    this.ad = e.ImmersiveInGameDisplayAd;
                    InvokeOnLoadedEvent(placement);
                }
            }

            private void OnAdLoadFailed(LoadAdError error) {
                IsLoading = false;

                InvokeOnLoadFailedEvent(placement, error.GetMessage());

                if (HasAutoLoad(LoadType.OnLoadFailed)) {
                    retry++;
                    float delay = 2 * Math.Min(6, retry);
                    IsLoading = true;
                    Invoke(Reload, delay);
                }
            }

            private void OnNativeAdClicked(object sender, System.EventArgs e) {
                InvokeOnClickedEvent(placement);
            }

            private void OnNativeAdImpression(object sender, System.EventArgs e) {

            }

            private void OnPaidEvent(AdValue value) {
                AdRevenuePaid adRevenuePaid = new AdRevenuePaid() {
                    adSource = client.Network.ToString(),
                    adNetwork = client.Network,
                    adUnitId = Id,
                    adFormat = Format,
                    placement = placement,
                    value = value.Value / 1000000d,
                    currency = value.CurrencyCode,
                };

                InvokeOnRevenuePaidEvent(adRevenuePaid);
            }
        }
#endif

    }
#endif

#if ADMOB
    public static class AdMobExtensions {

        public static float PixelToDp(int pixels) {
            float screenDensity = Screen.dpi / 160f;
            return pixels / screenDensity;
        }

        public static float DpToPixel(int dp) {
            float screenDensity = Screen.dpi / 160f;
            return dp * screenDensity;
        }

        public static AdPosition ToAdPosition(this AdPositions position) {
            switch (position) {
                case AdPositions.TopCenter: return AdPosition.Top;
                case AdPositions.TopLeft: return AdPosition.TopLeft;
                case AdPositions.TopRight: return AdPosition.TopRight;
                case AdPositions.Centered: return AdPosition.Center;
                case AdPositions.CenterLeft: return AdPosition.Center;
                case AdPositions.CenterRight: return AdPosition.Center;
                case AdPositions.BottomCenter: return AdPosition.Bottom;
                case AdPositions.BottomLeft: return AdPosition.BottomLeft;
                case AdPositions.BottomRight: return AdPosition.BottomRight;
                default: return AdPosition.Center;
            }
        }

        public static Vector2Int GetAdScreenPosition(this GoogleMobileAds.Api.NativeOverlayAd ad, AdPositions position, Vector2Int offset) {
            return GetAdScreenPositionInPixels(ad.GetTemplateWidthInPixels(), ad.GetTemplateHeightInPixels(), position, offset);
        }

        public static Vector2Int GetAdScreenPosition(this GoogleMobileAds.Api.NativeOverlayAd ad, AdPositions position, Vector2Int offset, AdSizeOption adSizeOption) {
            switch (adSizeOption.Type) {
                case AdSizeType.Custom: return GetAdScreenPositionInPixels(adSizeOption.Width, adSizeOption.Height, position, offset);
                default: return GetAdScreenPositionInPixels(ad.GetTemplateWidthInPixels(), ad.GetTemplateHeightInPixels(), position, offset);
            }
        }

        public static Vector2Int GetAdScreenPosition(this GoogleMobileAds.Api.BannerView ad, AdPositions position, Vector2Int offset) {
            return GetAdScreenPositionInPixels(ad.GetWidthInPixels(), ad.GetHeightInPixels(), position, offset);
        }

        public static Vector2Int GetAdScreenPositionInPixels(float adWidth, float adHeight, AdPositions position, Vector2Int offset) {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            float xOffset = offset.x;
            float yOffset = offset.y;

            float xPosition = 0;
            float yPosition = 0;

            switch (position) {
                case AdPositions.TopCenter:
                    xPosition = screenWidth / 2 - adWidth / 2;
                    yPosition = 0;
                    break;
                case AdPositions.TopLeft:
                    xPosition = 0;
                    yPosition = 0;
                    break;
                case AdPositions.TopRight:
                    xPosition = screenWidth - adWidth;
                    yPosition = 0;
                    break;
                case AdPositions.Centered:
                    xPosition = screenWidth / 2 - adWidth / 2;
                    yPosition = screenHeight / 2 - adHeight / 2;
                    break;
                case AdPositions.CenterLeft:
                    xPosition = 0;
                    yPosition = screenHeight / 2 - adHeight / 2;
                    break;
                case AdPositions.CenterRight:
                    xPosition = screenWidth - adWidth;
                    yPosition = screenHeight / 2 - adHeight / 2;
                    break;
                case AdPositions.BottomCenter:
                    xPosition = screenWidth / 2 - adWidth / 2;
                    yPosition = screenHeight - adHeight;
                    break;
                case AdPositions.BottomLeft:
                    xPosition = 0;
                    yPosition = screenHeight - adHeight;
                    break;
                case AdPositions.BottomRight:
                    xPosition = screenWidth - adWidth;
                    yPosition = screenHeight - adHeight;
                    break;
                default:
                    break;
            }

            xPosition += xOffset;
            yPosition += yOffset;

            float screenDensity = Screen.dpi / 160f;

            return new Vector2Int(Mathf.FloorToInt(xPosition / screenDensity), Mathf.FloorToInt(yPosition / screenDensity));
        }

        public static Vector2Int GetAdScreenPositionInDp(float adWidth, float adHeight, AdPositions position, Vector2Int offset) {
            float screenDensity = Screen.dpi / 160f;
            float screenWidth = Screen.width / screenDensity;
            float screenHeight = Screen.height / screenDensity;

            float xOffset = offset.x / screenDensity;
            float yOffset = offset.y / screenDensity;

            float xPosition = 0;
            float yPosition = 0;

            switch (position) {
                case AdPositions.TopCenter:
                    xPosition = screenWidth / 2 - adWidth / 2;
                    yPosition = 0;
                    break;
                case AdPositions.TopLeft:
                    xPosition = 0;
                    yPosition = 0;
                    break;
                case AdPositions.TopRight:
                    xPosition = screenWidth - adWidth;
                    yPosition = 0;
                    break;
                case AdPositions.Centered:
                    xPosition = screenWidth / 2 - adWidth / 2;
                    yPosition = screenHeight / 2 - adHeight / 2;
                    break;
                case AdPositions.CenterLeft:
                    xPosition = 0;
                    yPosition = screenHeight / 2 - adHeight / 2;
                    break;
                case AdPositions.CenterRight:
                    xPosition = screenWidth - adWidth;
                    yPosition = screenHeight / 2 - adHeight / 2;
                    break;
                case AdPositions.BottomCenter:
                    xPosition = screenWidth / 2 - adWidth / 2;
                    yPosition = screenHeight - adHeight;
                    break;
                case AdPositions.BottomLeft:
                    xPosition = 0;
                    yPosition = screenHeight - adHeight;
                    break;
                case AdPositions.BottomRight:
                    xPosition = screenWidth - adWidth;
                    yPosition = screenHeight - adHeight;
                    break;
                default:
                    break;
            }

            xPosition += xOffset;
            yPosition += yOffset;

            return new Vector2Int(Mathf.FloorToInt(xPosition), Mathf.FloorToInt(yPosition));
        }

        public static AdSize ToAdSize(this AdSizeOption adSize) {
            switch (adSize.Type) {
                case AdSizeType.Banner: return AdSize.Banner;
                case AdSizeType.MediumRectangle: return AdSize.MediumRectangle;
                case AdSizeType.IABBanner: return AdSize.IABBanner;
                case AdSizeType.Leaderboard: return AdSize.Leaderboard;
                case AdSizeType.AnchoredAdaptive: return AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
                default: return new AdSize(adSize.Width, adSize.Height);
            }
        }

        public static Vector2Int GetSizeInDp(this AdSizeOption adSize) {
            float screenDensity = Screen.dpi / 160f;

            switch (adSize.Type) {
                case AdSizeType.Banner: return new Vector2Int(320, 50);
                case AdSizeType.MediumRectangle: return new Vector2Int(300, 250);
                case AdSizeType.IABBanner: return new Vector2Int(468, 60);
                case AdSizeType.Leaderboard: return new Vector2Int(728, 90);
                case AdSizeType.AnchoredAdaptive: return new Vector2Int(Mathf.FloorToInt(Screen.width / screenDensity), Mathf.Min(90, Mathf.FloorToInt(0.15f * Screen.height / screenDensity)));
                default: return new Vector2Int(Mathf.FloorToInt(adSize.Width / screenDensity), Mathf.FloorToInt(adSize.Height / screenDensity));
            }
        }

        public static Vector2Int GetSizeInPixels(this AdSizeOption adSize) {
            float screenDensity = Screen.dpi / 160f;

            switch (adSize.Type) {
                case AdSizeType.Banner: return new Vector2Int(Mathf.FloorToInt(320 * screenDensity), Mathf.FloorToInt(50 * screenDensity));
                case AdSizeType.MediumRectangle: return new Vector2Int(Mathf.FloorToInt(300 * screenDensity), Mathf.FloorToInt(250 * screenDensity));
                case AdSizeType.IABBanner: return new Vector2Int(Mathf.FloorToInt(468 * screenDensity), Mathf.FloorToInt(60 * screenDensity));
                case AdSizeType.Leaderboard: return new Vector2Int(Mathf.FloorToInt(728 * screenDensity), Mathf.FloorToInt(90 * screenDensity));
                case AdSizeType.AnchoredAdaptive: return new Vector2Int(Screen.width, Mathf.Min(Mathf.FloorToInt(Screen.height * 0.15f), Mathf.FloorToInt(90 * screenDensity)));
                default: return new Vector2Int(adSize.Width, adSize.Height);
            }
        }

#if ADMOB_NATIVE_IMMERSIVE
        public static ImmersiveInGameDisplayAdAspectRatio ToAdAspectRation(this AdAspectRatio adAspectRatio) {
            switch (adAspectRatio.Type) {
                case AdAspectRatioType.AR_1x1: return new ImmersiveInGameDisplayAdAspectRatio(1, 1);
                case AdAspectRatioType.AR_6x5: return new ImmersiveInGameDisplayAdAspectRatio(6, 5);
                case AdAspectRatioType.AR_1x2: return new ImmersiveInGameDisplayAdAspectRatio(1, 2);
                case AdAspectRatioType.AR_32x5: return new ImmersiveInGameDisplayAdAspectRatio(32, 5);
                case AdAspectRatioType.AR_16x5: return new ImmersiveInGameDisplayAdAspectRatio(16, 5);
                case AdAspectRatioType.AR_364x45: return new ImmersiveInGameDisplayAdAspectRatio(364, 45);
                case AdAspectRatioType.AR_4x5: return new ImmersiveInGameDisplayAdAspectRatio(4, 5);
                case AdAspectRatioType.AR_97x25: return new ImmersiveInGameDisplayAdAspectRatio(97, 25);
                case AdAspectRatioType.AR_19x10: return new ImmersiveInGameDisplayAdAspectRatio(19, 10);
                case AdAspectRatioType.AR_39x5: return new ImmersiveInGameDisplayAdAspectRatio(39, 5);
                default: return new ImmersiveInGameDisplayAdAspectRatio(adAspectRatio.Width, adAspectRatio.Height);
            }
        }
#endif
    }
#endif

    [CategoryMenu("AdMob Advertisings")]
    [System.Serializable]
    public class AdMobServiceProvider : AdServiceProvider {
        public override AdService GetService() {
#if ADMOB
            return new AdMobAdvertisings();
#else
            return null;
#endif
        }
    }
}

