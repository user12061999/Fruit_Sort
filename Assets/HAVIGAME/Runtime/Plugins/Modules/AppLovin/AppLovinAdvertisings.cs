using System;
using UnityEngine;
using HAVIGAME.Services.Advertisings;

namespace HAVIGAME.Plugins.AppLovin {
#if APPLOVIN
    public class AppLovinAdvertisings : AdService {
        private AdSizeOption bannerAdSize;
        public override AdNetwork Network => AdNetwork.AppLovin;
        public override HAVIGAME.Services.Advertisings.AdFormat AdFormatSupported => HAVIGAME.Services.Advertisings.AdFormat.AppLovinAdUnits;

        protected override Services.Advertisings.BannerAd CreateBannerAd(AdId id) {
            AppLovinBannerAd ad = new AppLovinBannerAd(this, id.Clone(), bannerAdSize);
            BannerAds.Add(ad);
            return ad;
        }

        protected override Services.Advertisings.InterstitialAd CreateInterstitialAd(AdId id) {
            AppLovinInterstitialAd ad = new AppLovinInterstitialAd(this, id.Clone());
            InterstitialAds.Add(ad);
            return ad;
        }

        protected override MediumRectangleAd CreateMediumRectangleAd(AdId id) {
            AppLovinMediumRectangleAd ad = new AppLovinMediumRectangleAd(this, id.Clone());
            MediumRectangleAds.Add(ad);
            return ad;
        }

        protected override Services.Advertisings.RewardedAd CreateRewardedAd(AdId id) {
            AppLovinRewardedAd ad = new AppLovinRewardedAd(this, id.Clone());
            RewardedAds.Add(ad);
            return ad;
        }

        public override void Initialize() {
            if (InitializeEvent.IsRunning) {
                Log.Warning("[AppLovinAdvertisings] AppLovin advertisings is running with initialize state {0}.", IsInitialized);
                return;
            }

            AppLovinManager.initializeEvent.AddListener(OnAppLovinInitializedCallback);
        }

        private void OnAppLovinInitializedCallback(bool isInitialized) {

            if (isInitialized) {

                AppLovinSetting settings = AppLovinSetting.Instance;
                bannerAdSize = settings.BannerAdSize;

                if (!IsRemoveAdFormat(Services.Advertisings.AdFormat.AppOpenAd)) {
                    AppOpenAds = new AdCollection<Services.Advertisings.AppOpenAd>();

                    if (settings.AppOpenAdIds != null && settings.AppOpenAdIds.Length > 0) {
                        for (int i = 0; i < settings.AppOpenAdIds.Length; i++) {
                            AppLovinAppOpenAd appOpenAd = new AppLovinAppOpenAd(this, settings.AppOpenAdIds[i]);
                            AppOpenAds.Add(appOpenAd);
                        }
                    }
                }

                if (!IsRemoveAdFormat(Services.Advertisings.AdFormat.InterstitialAd)) {
                    InterstitialAds = new AdCollection<Services.Advertisings.InterstitialAd>();

                    if (settings.InterstitialAdIds != null && settings.InterstitialAdIds.Length > 0) {
                        for (int i = 0; i < settings.InterstitialAdIds.Length; i++) {
                            AppLovinInterstitialAd interstitialAd = new AppLovinInterstitialAd(this, settings.InterstitialAdIds[i]);
                            InterstitialAds.Add(interstitialAd);
                        }
                    }
                }

                if (!IsRemoveAdFormat(Services.Advertisings.AdFormat.RewardedAd)) {
                    RewardedAds = new AdCollection<Services.Advertisings.RewardedAd>();

                    if (settings.RewardedAdIds != null && settings.RewardedAdIds.Length > 0) {
                        for (int i = 0; i < settings.RewardedAdIds.Length; i++) {
                            AppLovinRewardedAd rewardedAd = new AppLovinRewardedAd(this, settings.RewardedAdIds[i]);
                            RewardedAds.Add(rewardedAd);
                        }
                    }
                }

                if (!IsRemoveAdFormat(Services.Advertisings.AdFormat.BannerAd)) {
                    BannerAds = new AdCollection<Services.Advertisings.BannerAd>();

                    if (settings.BannerAdIds != null && settings.BannerAdIds.Length > 0) {
                        for (int i = 0; i < settings.BannerAdIds.Length; i++) {
                            AppLovinBannerAd bannerAd = new AppLovinBannerAd(this, settings.BannerAdIds[i], bannerAdSize);
                            BannerAds.Add(bannerAd);
                        }
                    }
                }

                if (!IsRemoveAdFormat(Services.Advertisings.AdFormat.MediumRectangleAd)) {
                    MediumRectangleAds = new AdCollection<Services.Advertisings.MediumRectangleAd>();

                    if (settings.MediumRectangleAdIds != null && settings.MediumRectangleAdIds.Length > 0) {
                        for (int i = 0; i < settings.MediumRectangleAdIds.Length; i++) {
                            AppLovinMediumRectangleAd mediumRectangle = new AppLovinMediumRectangleAd(this, settings.MediumRectangleAdIds[i]);
                            MediumRectangleAds.Add(mediumRectangle);
                        }
                    }
                }

                Log.Debug("[AppLovinAdvertisings] AppLovin advertisings initialize completed.");
                InitializeEvent.Invoke(true);

                Database.Unload(settings);

                if (MaxSdk.IsVerboseLoggingEnabled()) {
                    MaxSdk.ShowMediationDebugger();
                }
            } else {
                Log.Error("[AppLovinAdvertisings] AppLovin advertisings initialize failed because AppLovin initialize failed");
                InitializeEvent.Invoke(false);
            }
        }

        private class AppLovinAppOpenAd : AppOpenAd {
            private AppLovinAdvertisings client;
            private string placement;
            private Action onCompleted;
            private int retry;

            public override AdService Client => client;
            public override bool IsReady {
                get {
                    return MaxSdk.IsAppOpenAdReady(Id);
                }
            }

            public AppLovinAppOpenAd(AppLovinAdvertisings client, AdId id) : base(id) {
                this.client = client;
                this.retry = 0;
                this.placement = "NULL";

                MaxSdkCallbacks.AppOpen.OnAdClickedEvent += OnAdClickedEvent;
                MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAdDisplayedEvent;
                MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
                MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAdHiddenEvent;
                MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAdLoadedEvent;
                MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAdLoadFailedEvent;
                MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;

                if (HasAutoLoad(LoadType.OnCreated)) {
                    Load();
                }
            }

            public override Ad Clone() {
                Log.Warning("[AppLovinAppOpenAd] Not support!");
                return null;
            }

            public override bool Load() {
                if (IsReady) {
                    Log.Warning("[AppLovinAppOpenAd] Load failed! App open ad is ready!");
                    return false;
                }
                
                if (IsReloading) {
                    Log.Warning("[AppLovinAppOpenAd] Load failed! App open ad is reloading!");
                    return false;
                }

                if (IsLoading) {
                    Log.Warning("[AppLovinAppOpenAd] Load failed! App open ad is loading!");
                    return false;
                }

                if (string.IsNullOrEmpty(Id)) {
                    Log.Warning("[AppLovinAppOpenAd] Load failed! App open ad id is null or empty!");
                    return false;
                }

                IsLoading = true;
                InvokeOnLoadEvent(placement);
                MaxSdk.LoadAppOpenAd(Id);
                return true;
            }

            public override bool Show(Action onCompleted, string placement) {
                if (!IsReady) {
                    Log.Warning("[AppLovinAppOpenAd] Show failed! App open ad not avaliable!");
                    return false;
                }

                IsShowing = true;
                this.onCompleted = onCompleted;
                this.placement = placement;

                InvokeOnDisplayEvent(placement);

                MaxSdk.ShowAppOpenAd(Id, placement);
                return true;
            }

            public override bool Destroy() {
                return false;
            }

            private void OnAdRevenuePaidEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                AdRevenuePaid adRevenuePaid = new AdRevenuePaid() {
                    adSource = info.NetworkName,
                    adNetwork = client.Network,
                    adUnitId = info.AdUnitIdentifier,
                    adFormat = Format,
                    placement = placement,
                    value = info.Revenue,
                    currency = "USD",
                };

                InvokeOnRevenuePaidEvent(adRevenuePaid);
            }

            private void OnAdLoadFailedEvent(string id, MaxSdkBase.ErrorInfo error) {
                if (!Id.Equals(id)) return;

                IsLoading = false;
                InvokeOnLoadFailedEvent(placement, error.Message);

                if (HasAutoLoad(LoadType.OnLoadFailed)) {
                    retry++;
                    float delay = 2 * Math.Min(6, retry);
                    IsReloading = true;
                    Invoke(Reload, delay);
                }
            }

            private void OnAdLoadedEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                IsLoading = false;
                InvokeOnLoadedEvent(placement);

                retry = 0;
            }

            private void OnAdDisplayFailedEvent(string id, MaxSdkBase.ErrorInfo error, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                IsShowing = false;
                Invoke(onCompleted);
                InvokeOnDisplayFailedEvent(placement, error.Message);

                if (HasAutoLoad(LoadType.OnDisplayFailed)) {
                    Reload(true);
                }
            }

            private void OnAdDisplayedEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                InvokeOnDisplayedEvent(placement);
            }

            private void OnAdClickedEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                InvokeOnClickedEvent(placement);
            }

            private void OnAdHiddenEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                IsShowing = false;
                Invoke(onCompleted);
                InvokeOnClosedEvent(placement);

                if (HasAutoLoad(LoadType.OnClosed)) {
                    Reload(true);
                }
            }
        }

        private class AppLovinInterstitialAd : InterstitialAd {
            private AppLovinAdvertisings client;
            private string placement;
            private Action onCompleted;
            private int retry;

            public override AdService Client => client;
            public override bool IsReady {
                get {
                    return MaxSdk.IsInterstitialReady(Id);
                }
            }

            public AppLovinInterstitialAd(AppLovinAdvertisings client, AdId id) : base(id) {
                this.client = client;
                this.retry = 0;
                this.placement = "NULL";

                MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnAdLoadedEvent;
                MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnAdLoadFailedEvent;
                MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnAdDisplayedEvent;
                MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
                MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnAdClickedEvent;
                MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnAdHiddenEvent;
                MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;

                if (HasAutoLoad(LoadType.OnCreated)) {
                    Load();
                }
            }

            public override Ad Clone() {
                Log.Warning("[AppLovinAppOpenAd] Not support!");
                return null;
            }

            public override bool Load() {
                if (IsReady) {
                    Log.Warning("[AppLovinInterstitialAd] Load failed! Interstitial ad is ready!");
                    return false;
                }
                
                if (IsReloading) {
                    Log.Warning("[AppLovinInterstitialAd] Load failed! Interstitial ad is reloading!");
                    return false;
                }

                if (IsLoading) {
                    Log.Warning("[AppLovinInterstitialAd] Load failed! Interstitial ad is loading!");
                    return false;
                }

                if (string.IsNullOrEmpty(Id)) {
                    Log.Warning("[AppLovinInterstitialAd] Load failed! Interstitial ad id is null or empty!");
                    return false;
                }

                IsLoading = true;
                InvokeOnLoadEvent(placement);

                MaxSdk.LoadInterstitial(Id);
                return true;
            }

            public override bool Show(Action onCompleted, string placement) {
                if (!IsReady) {
                    Log.Warning("[AppLovinInterstitialAd] Show failed! Interstitial ad not avaliable!");
                    return false;
                }

                IsShowing = true;
                this.onCompleted = onCompleted;
                this.placement = placement;

                InvokeOnDisplayEvent(placement);

                MaxSdk.ShowInterstitial(Id, placement);
                return true;
            }

            public override bool Destroy() {
                return false;
            }

            private void OnAdLoadedEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                IsLoading = false;
                InvokeOnLoadedEvent(placement);

                retry = 0;
            }

            private void OnAdLoadFailedEvent(string id, MaxSdkBase.ErrorInfo error) {
                if (!Id.Equals(id)) return;

                IsLoading = false;
                InvokeOnLoadFailedEvent(placement, error.Message);

                if (HasAutoLoad(LoadType.OnLoadFailed)) {
                    retry++;
                    float delay = 2 * Math.Min(6, retry);
                    IsReloading = true;
                    Invoke(Reload, delay);
                }
            }

            private void OnAdDisplayedEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                InvokeOnDisplayedEvent(placement);
            }

            private void OnAdDisplayFailedEvent(string id, MaxSdkBase.ErrorInfo error, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                IsShowing = false;
                Invoke(onCompleted);
                InvokeOnDisplayFailedEvent(placement, error.Message);

                if (HasAutoLoad(LoadType.OnDisplayFailed)) {
                    Reload(true);
                }
            }

            private void OnAdClickedEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                InvokeOnClickedEvent(placement);
            }

            private void OnAdHiddenEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                IsShowing = false;
                Invoke(onCompleted);
                InvokeOnClosedEvent(placement);

                if (HasAutoLoad(LoadType.OnClosed)) {
                    Reload(true);
                }
            }

            private void OnAdRevenuePaidEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                AdRevenuePaid adRevenuePaid = new AdRevenuePaid() {
                    adSource = info.NetworkName,
                    adNetwork = client.Network,
                    adUnitId = info.AdUnitIdentifier,
                    adFormat = Format,
                    placement = placement,
                    value = info.Revenue,
                    currency = "USD",
                };

                InvokeOnRevenuePaidEvent(adRevenuePaid);
            }
        }

        private class AppLovinRewardedAd : RewardedAd {
            private AppLovinAdvertisings client;
            private string placement;
            private Action onCompleted;
            private Action onFailed;
            private int retry;
            private bool receivedReward;

            public override AdService Client => client;
            public override bool IsReady {
                get {
                    return MaxSdk.IsRewardedAdReady(Id);
                }
            }

            public AppLovinRewardedAd(AppLovinAdvertisings client, AdId id) : base(id) {
                this.client = client;

                this.retry = 0;
                this.placement = "NULL";
                this.receivedReward = false;

                MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnAdLoadedEvent;
                MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnAdLoadFailedEvent;
                MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnAdDisplayedEvent;
                MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
                MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnAdClickedEvent;
                MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnAdReceivedRewardEvent;
                MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnAdHiddenEvent;
                MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;

                if (HasAutoLoad(LoadType.OnCreated)) {
                    Load();
                }
            }

            public override Ad Clone() {
                Log.Warning("[AppLovinAppOpenAd] Not support!");
                return null;
            }

            public override bool Load() {
                if (IsReady) {
                    Log.Warning("[AppLovinRewardedAd] Load failed! Rearded ad is ready!");
                    return false;
                }
                
                if (IsReloading) {
                    Log.Warning("[AppLovinRewardedAd] Load failed! Rearded ad is reloading!");
                    return false;
                }

                if (IsLoading) {
                    Log.Warning("[AppLovinRewardedAd] Load failed! Rearded ad is loading!");
                    return false;
                }

                if (string.IsNullOrEmpty(Id)) {
                    Log.Warning("[AppLovinRewardedAd] Load failed! Rearded ad id is null or empty!");
                    return false;
                }

                IsLoading = true;
                InvokeOnLoadEvent(placement);

                MaxSdk.LoadRewardedAd(Id);

                return true;
            }

            public override bool Show(Action onCompleted, Action onFailed, string placement) {
                if (!IsReady) {
                    Log.Warning("[AppLovinRewardedAd] Show failed! Rearded ad not avaliable!");
                    return false;
                }

                receivedReward = false;
                IsShowing = true;
                this.onCompleted = onCompleted;
                this.onFailed = onFailed;
                this.placement = placement;

                InvokeOnDisplayEvent(placement);

                MaxSdk.ShowRewardedAd(Id, placement);
                return true;
            }

            public override bool Destroy() {
                return false;
            }

            private void OnAdLoadedEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                IsLoading = false;
                InvokeOnLoadedEvent(placement);

                retry = 0;
            }

            private void OnAdLoadFailedEvent(string id, MaxSdkBase.ErrorInfo error) {
                if (!Id.Equals(id)) return;

                IsLoading = false;
                InvokeOnLoadFailedEvent(placement, error.Message);

                if (HasAutoLoad(LoadType.OnLoadFailed)) {
                    retry++;
                    float delay = 2 * Math.Min(6, retry);
                    IsReloading = true;
                    Invoke(Reload, delay);
                }
            }

            private void OnAdDisplayedEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                InvokeOnDisplayedEvent(placement);
            }

            private void OnAdDisplayFailedEvent(string id, MaxSdkBase.ErrorInfo error, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                IsShowing = false;
                receivedReward = false;
                Invoke(onFailed);
                InvokeOnDisplayFailedEvent(placement, error.Message);

                if (HasAutoLoad(LoadType.OnDisplayFailed)) {
                    Reload(true);
                }
            }

            private void OnAdClickedEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                InvokeOnClickedEvent(placement);
            }

            private void OnAdHiddenEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                IsShowing = false;
                CheckReceiveRewardProcess();

                InvokeOnClosedEvent(placement);

                if (HasAutoLoad(LoadType.OnClosed)) {
                    Reload(true);
                }
            }

            private void OnAdReceivedRewardEvent(string id, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                receivedReward = true;
                CheckReceiveRewardProcess();
            }

            private void OnAdRevenuePaidEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                AdRevenuePaid adRevenuePaid = new AdRevenuePaid() {
                    adSource = info.NetworkName,
                    adNetwork = client.Network,
                    adUnitId = info.AdUnitIdentifier,
                    adFormat = Format,
                    placement = placement,
                    value = info.Revenue,
                    currency = "USD",
                };

                InvokeOnRevenuePaidEvent(adRevenuePaid);
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

        private class AppLovinBannerAd : BannerAd {
            private AppLovinAdvertisings client;
            private string placement;

            public override AdService Client => client;
            public override bool IsReady {
                get {
                    return true;
                }
            }
            public override float Width {
                get {
                    if (IsShowing) {
                        Rect bannerRect = MaxSdk.GetBannerLayout(Id);
                        return AppLovinExtensions.ConvertDpToPixels(bannerRect.width);
                    } else {
                        return 0;
                    }
                }
            }
            public override float Height {
                get {
                    if (IsShowing) {
                        Rect bannerRect = MaxSdk.GetBannerLayout(Id);
                        return AppLovinExtensions.ConvertDpToPixels(bannerRect.height);
                    } else {
                        return 0;
                    }
                }
            }

            public AppLovinBannerAd(AppLovinAdvertisings client, AdId id, AdSizeOption size) : base(id, true, size) {
                this.client = client;
                this.placement = "NULL";

                MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnAdLoadedEvent;
                MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnAdLoadFailedEvent;
                MaxSdkCallbacks.Banner.OnAdClickedEvent += OnAdClickedEvent;
                MaxSdkCallbacks.Banner.OnAdCollapsedEvent += OnAdCollapsedEvent;
                MaxSdkCallbacks.Banner.OnAdExpandedEvent += OnAdExpandedEvent;
                MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;

                if (HasAutoLoad(LoadType.OnCreated)) {
                    Load();
                }
            }

            public override Ad Clone() {
                Log.Warning("[AppLovinAppOpenAd] Not support!");
                return null;
            }

            public override bool Load() {
                return InternalCreate(Position, Offset);
            }

            private bool InternalCreate(AdPositions position, Vector2Int offset) {
                if (Created) {
                    Log.Warning("[AppLovinBannerAd] Banner ad has created!");
                    return false;
                }

                if (string.IsNullOrEmpty(Id)) {
                    Log.Warning("[AppLovinBannerAd] Create ad failed! Banner ad id is null or empty!");
                    return false;
                }

                Created = true;
                Position = position;
                Offset = offset;

                MaxSdk.CreateBanner(Id, position.ToAdPosition());
                MaxSdk.SetBannerExtraParameter(Id, "adaptive_banner", Size.Type == AdSizeType.AnchoredAdaptive ? "true" : "false");
                MaxSdk.SetBannerBackgroundColor(Id, new Color(1, 1, 1, 0));

                if (Offset == Vector2Int.zero) {
                    MaxSdk.UpdateBannerPosition(Id, Position.ToAdPosition());
                } else {
                    Vector2Int adScreenPosition = AppLovinExtensions.GetBannerAdScreenPosition(Position, offset);
                    MaxSdk.UpdateBannerPosition(Id, adScreenPosition.x, adScreenPosition.y);
                }

                MaxSdk.HideBanner(Id);

                return true;
            }

            public override bool Show(AdPositions position, Vector2Int offset, string placement) {
                if (!Created) {
                    Log.Warning("[AppLovinBannerAd] Banner ad not been created! Create a new banner ad.");
                    if (!InternalCreate(position, offset)) {
                        return false;
                    }
                }

                if (Position != position || Offset != offset) {

                    if (offset == Vector2Int.zero) {
                        MaxSdk.UpdateBannerPosition(Id, position.ToAdPosition());
                    } else {
                        Vector2Int adScreenPosition = AppLovinExtensions.GetBannerAdScreenPosition(position, Offset);
                        MaxSdk.UpdateBannerPosition(Id, adScreenPosition.x, adScreenPosition.y);
                    }
                }

                IsLoading = true;
                IsShowing = true;
                this.Position = position;
                this.Offset = offset;
                this.placement = placement;

                InvokeOnDisplayEvent(placement);

                MaxSdk.ShowBanner(Id);

                InvokeOnDisplayedEvent(placement);

                return true;
            }

            public override void SetPosition(AdPositions position, Vector2Int offset) {
                if (position != Position || offset != Offset) {
                    this.Position = position;
                    this.Offset = offset;

                    if (IsReady) {
                        if (offset == Vector2Int.zero) {
                            MaxSdk.UpdateBannerPosition(Id, Position.ToAdPosition());
                        } else {
                            Vector2Int adScreenPosition = AppLovinExtensions.GetBannerAdScreenPosition(Position, Offset);
                            MaxSdk.UpdateBannerPosition(Id, adScreenPosition.x, adScreenPosition.y);
                        }
                    }
                }
            }

            public override bool Hide() {
                if (!IsShowing) {
                    Log.Warning("[AppLovinBannerAd] Banner ad not showing!");
                    return false;
                }

                IsShowing = false;
                MaxSdk.HideBanner(Id);

                InvokeOnClosedEvent(placement);

                return true;
            }

            public override bool Destroy() {
                if (!Created) {
                    Log.Warning("[AppLovinBannerAd] Banner ad not been created!");
                    return false;
                }

                Created = false;

                if (IsShowing) {
                    IsShowing = false;

                    InvokeOnClosedEvent(placement);
                }
                
                this.Position = AdPositions.Null;
                this.Offset = Vector2Int.zero;

                MaxSdk.DestroyBanner(Id);
                return true;
            }

            private void OnAdLoadedEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                IsLoading = false;
                InvokeOnLoadedEvent(placement);
            }

            private void OnAdLoadFailedEvent(string id, MaxSdkBase.ErrorInfo error) {
                if (!Id.Equals(id)) return;

                InvokeOnLoadFailedEvent(placement, error.Message);
            }

            private void OnAdClickedEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                InvokeOnClickedEvent(placement);
            }

            private void OnAdExpandedEvent(string id, MaxSdkBase.AdInfo info) {
            }

            private void OnAdCollapsedEvent(string id, MaxSdkBase.AdInfo info) {
            }

            private void OnAdRevenuePaidEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                AdRevenuePaid adRevenuePaid = new AdRevenuePaid() {
                    adSource = info.NetworkName,
                    adNetwork = client.Network,
                    adUnitId = info.AdUnitIdentifier,
                    adFormat = Format,
                    placement = placement,
                    value = info.Revenue,
                    currency = "USD",
                };

                InvokeOnRevenuePaidEvent(adRevenuePaid);
            }

        }

        private class AppLovinMediumRectangleAd : MediumRectangleAd {
            private AppLovinAdvertisings client;
            private string placement;

            public override AdService Client => client;
            public override bool IsReady {
                get {
                    return true;
                }
            }
            public override float Width {
                get {
                    if (IsShowing) {
                        Rect adRect = MaxSdk.GetMRecLayout(Id);
                        return AppLovinExtensions.ConvertDpToPixels(adRect.width);
                    } else {
                        return 0;
                    }
                }
            }
            public override float Height {
                get {
                    if (IsShowing) {
                        Rect adRect = MaxSdk.GetMRecLayout(Id);
                        return AppLovinExtensions.ConvertDpToPixels(adRect.height);
                    } else {
                        return 0;
                    }
                }
            }

            public AppLovinMediumRectangleAd(AppLovinAdvertisings client, AdId id) : base(id) {
                this.client = client;
                this.placement = "NULL";

                MaxSdkCallbacks.MRec.OnAdLoadedEvent += OnAdLoadedEvent;
                MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += OnAdLoadFailedEvent;
                MaxSdkCallbacks.MRec.OnAdClickedEvent += OnAdClickedEvent;
                MaxSdkCallbacks.MRec.OnAdCollapsedEvent += OnAdCollapsedEvent;
                MaxSdkCallbacks.MRec.OnAdExpandedEvent += OnAdExpandedEvent;
                MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;

                if (HasAutoLoad(LoadType.OnCreated)) {
                    Load();
                }
            }

            public override Ad Clone() {
                Log.Warning("[AppLovinAppOpenAd] Not support!");
                return null;
            }

            public override bool Load() {
                if (Created) {
                    Log.Warning("[AppLovinMediumRectangleAd] Medium rectangle ad has created!");
                    return false;
                }

                if (string.IsNullOrEmpty(Id)) {
                    Log.Warning("[AppLovinMediumRectangleAd] Create ad failed! Medium rectangle ad id is null or empty!");
                    return false;
                }

                Created = true;
                MaxSdk.CreateMRec(Id, AdPositions.BottomCenter.ToAdViewPosition());
                MaxSdk.HideBanner(Id);
                return true;
            }

            public override bool Show(AdPositions position, Vector2Int offset, string placement) {
                if (!Created) {
                    Log.Warning("[AppLovinMediumRectangleAd] Medium rectangle ad not been created! Create a new medium rectangle ad.");
                    if (!Load()) {
                        return false;
                    }
                }

                if (Position != position || Offset != offset) {
                    if (offset == Vector2Int.zero) {
                        MaxSdk.UpdateMRecPosition(Id, position.ToAdViewPosition());
                    } else {
                        Vector2Int adScreenPosition = AppLovinExtensions.GetBannerAdScreenPosition(position, offset);
                        MaxSdk.UpdateMRecPosition(Id, adScreenPosition.x, adScreenPosition.y);
                    }
                }

                IsLoading = true;
                IsShowing = true;
                this.Offset = offset;
                this.Position = position;
                this.placement = placement;

                InvokeOnDisplayEvent(placement);

                MaxSdk.ShowMRec(Id);
                MaxSdk.StartMRecAutoRefresh(Id);

                InvokeOnDisplayedEvent(placement);

                return true;
            }

            public override void SetPosition(AdPositions position, Vector2Int offset) {
                if (position != Position || offset != Offset) {
                    this.Position = position;
                    this.Offset = offset;

                    if (IsReady) {
                        if (offset == Vector2Int.zero) {
                            MaxSdk.UpdateMRecPosition(Id, position.ToAdViewPosition());
                        } else {
                            Vector2Int adScreenPosition = AppLovinExtensions.GetBannerAdScreenPosition(position, offset);
                            MaxSdk.UpdateMRecPosition(Id, adScreenPosition.x, adScreenPosition.y);
                        }
                    }
                }
            }
            public override bool Hide() {
                if (!IsShowing) {
                    Log.Warning("[AppLovinMediumRectangleAd] Medium rectangle ad not showing!");
                    return false;
                }

                IsShowing = false;
                MaxSdk.HideMRec(Id);

                InvokeOnClosedEvent(placement);

                return true;
            }

            public override bool Destroy() {
                if (!Created) {
                    Log.Warning("[AppLovinMediumRectangleAd] Medium rectangle ad not been created!");
                    return false;
                }

                Created = false;

                if (IsShowing) {
                    IsShowing = false;

                    InvokeOnClosedEvent(placement);
                }

                this.Position = AdPositions.Null;
                this.Offset = Vector2Int.zero;

                MaxSdk.DestroyMRec(Id);
                return true;
            }

            private void OnAdLoadedEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                IsLoading = false;
                InvokeOnLoadedEvent(placement);
            }

            private void OnAdLoadFailedEvent(string id, MaxSdkBase.ErrorInfo error) {
                if (!Id.Equals(id)) return;

                InvokeOnLoadFailedEvent(placement, error.Message);
            }

            private void OnAdClickedEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                InvokeOnClickedEvent(placement);
            }

            private void OnAdExpandedEvent(string id, MaxSdkBase.AdInfo info) {
            }

            private void OnAdCollapsedEvent(string id, MaxSdkBase.AdInfo info) {
            }

            private void OnAdRevenuePaidEvent(string id, MaxSdkBase.AdInfo info) {
                if (!Id.Equals(id)) return;

                AdRevenuePaid adRevenuePaid = new AdRevenuePaid() {
                    adSource = info.NetworkName,
                    adNetwork = client.Network,
                    adUnitId = info.AdUnitIdentifier,
                    adFormat = Format,
                    placement = placement,
                    value = info.Revenue,
                    currency = "USD",
                };

                InvokeOnRevenuePaidEvent(adRevenuePaid);
            }

        }
    }
#endif


#if APPLOVIN
    public static class AppLovinExtensions {
        public static float ConvertDpToPixels(float dp) {
            return dp * (Screen.dpi / 160f);
        }

        public static float ConverPixelsToDp(float pixel) {
            return pixel / (Screen.dpi / 160f);
        }

        public static MaxSdk.BannerPosition ToAdPosition(this AdPositions position) {
            switch (position) {
                case AdPositions.TopCenter: return MaxSdk.BannerPosition.TopCenter;
                case AdPositions.TopLeft: return MaxSdk.BannerPosition.TopLeft;
                case AdPositions.TopRight: return MaxSdk.BannerPosition.TopRight;
                case AdPositions.Centered: return MaxSdk.BannerPosition.Centered;
                case AdPositions.CenterLeft: return MaxSdk.BannerPosition.CenterLeft;
                case AdPositions.CenterRight: return MaxSdk.BannerPosition.CenterRight;
                case AdPositions.BottomCenter: return MaxSdk.BannerPosition.BottomCenter;
                case AdPositions.BottomLeft: return MaxSdk.BannerPosition.BottomLeft;
                case AdPositions.BottomRight: return MaxSdk.BannerPosition.BottomRight;
                default: return MaxSdk.BannerPosition.Centered;
            }
        }

        public static MaxSdkBase.AdViewPosition ToAdViewPosition(this AdPositions position) {
            switch (position) {
                case AdPositions.TopCenter: return MaxSdkBase.AdViewPosition.TopCenter;
                case AdPositions.TopLeft: return MaxSdkBase.AdViewPosition.TopLeft;
                case AdPositions.TopRight: return MaxSdkBase.AdViewPosition.TopRight;
                case AdPositions.Centered: return MaxSdkBase.AdViewPosition.Centered;
                case AdPositions.CenterLeft: return MaxSdkBase.AdViewPosition.CenterLeft;
                case AdPositions.CenterRight: return MaxSdkBase.AdViewPosition.CenterRight;
                case AdPositions.BottomCenter: return MaxSdkBase.AdViewPosition.BottomCenter;
                case AdPositions.BottomLeft: return MaxSdkBase.AdViewPosition.BottomLeft;
                case AdPositions.BottomRight: return MaxSdkBase.AdViewPosition.BottomRight;
                default: return MaxSdkBase.AdViewPosition.Centered;
            }
        }

        public static Vector2Int GetBannerAdScreenPosition(AdPositions position, Vector2Int offset) {
            return GetAdScreenPosition(320, 50, position, offset);
        }

        public static Vector2Int GetMRectAdScreenPosition(AdPositions position, Vector2Int offset) {
            return GetAdScreenPosition(300, 250, position, offset);
        }

        public static Vector2Int GetAdScreenPosition(float widthInPixels, float heightInPixels, AdPositions position, Vector2Int offset) {
            float screenDensity = MaxSdkUtils.GetScreenDensity();

            float screenWidthDp = Screen.width / screenDensity;
            float screenHeightDp = Screen.height / screenDensity;

            float adWidthDp = widthInPixels;
            float adHeightDp = heightInPixels;

            float xOffsetDp = offset.x / screenDensity;
            float yOffsetDp = offset.y / screenDensity;

            float xPositionDp = 0;
            float yPositionDp = 0;

            switch (position) {
                case AdPositions.TopCenter:
                    xPositionDp = screenWidthDp / 2 - adWidthDp / 2;
                    yPositionDp = 0;
                    break;
                case AdPositions.TopLeft:
                    xPositionDp = 0;
                    yPositionDp = 0;
                    break;
                case AdPositions.TopRight:
                    xPositionDp = screenWidthDp - adWidthDp;
                    yPositionDp = 0;
                    break;
                case AdPositions.Centered:
                    xPositionDp = screenWidthDp / 2 - adWidthDp / 2;
                    yPositionDp = screenHeightDp / 2 - adHeightDp / 2;
                    break;
                case AdPositions.CenterLeft:
                    xPositionDp = 0;
                    yPositionDp = screenHeightDp / 2 - adHeightDp / 2;
                    break;
                case AdPositions.CenterRight:
                    xPositionDp = screenWidthDp - adWidthDp;
                    yPositionDp = screenHeightDp / 2 - adHeightDp / 2;
                    break;
                case AdPositions.BottomCenter:
                    xPositionDp = screenWidthDp / 2 - adWidthDp / 2;
                    yPositionDp = screenHeightDp - adHeightDp;
                    break;
                case AdPositions.BottomLeft:
                    xPositionDp = 0;
                    yPositionDp = screenHeightDp - adHeightDp;
                    break;
                case AdPositions.BottomRight:
                    xPositionDp = screenWidthDp - adWidthDp;
                    yPositionDp = screenHeightDp - adHeightDp;
                    break;
                default:
                    break;
            }

            xPositionDp += xOffsetDp;
            yPositionDp += yOffsetDp;

            return new Vector2Int(Mathf.FloorToInt(xPositionDp), Mathf.FloorToInt(yPositionDp));
        }
    }
#endif

    [CategoryMenu("AppLovin Advertisings")]
    [System.Serializable]
    public class AppLovinAdServiceProvider : AdServiceProvider {
        public override AdService GetService() {
#if APPLOVIN
            return new AppLovinAdvertisings();
#else
            return null;
#endif
        }
    }
}

