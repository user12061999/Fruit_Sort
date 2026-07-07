using System;
using UnityEngine;
using HAVIGAME.Services.Advertisings;


#if IRON_SOURCE
using com.unity3d.mediation;
#endif

namespace HAVIGAME.Plugins.IronSources {
#if IRON_SOURCE
    public class IronSourceAdvertisings : AdService {
        private AdSizeOption bannerAdSize;

        public override HAVIGAME.Services.Advertisings.AdNetwork Network => HAVIGAME.Services.Advertisings.AdNetwork.IronSource;
        public override HAVIGAME.Services.Advertisings.AdFormat AdFormatSupported => HAVIGAME.Services.Advertisings.AdFormat.IronSourceAdUnits;

        protected override Services.Advertisings.InterstitialAd CreateInterstitialAd(AdId id) {
            IronSourceInterstitialAd ad = new IronSourceInterstitialAd(this, id.Clone());
            InterstitialAds.Add(ad);
            return ad;
        }

        protected override Services.Advertisings.RewardedAd CreateRewardedAd(AdId id) {
            IronSourceRewardedAd ad = new IronSourceRewardedAd(this, id.Clone());
            RewardedAds.Add(ad);
            return ad;
        }

        protected override Services.Advertisings.BannerAd CreateBannerAd(AdId id) {
            IronSourceBannerAd ad = new IronSourceBannerAd(this, id.Clone());
            BannerAds.Add(ad);
            return ad;
        }

        public override void Initialize() {
            if (InitializeEvent.IsRunning) {
                Log.Warning("[IronSourceAdvertisings] IronSource advertisings is running with initialize state {0}.", IsInitialized);
                return;
            }

            IronSourceManager.initializeEvent.AddListener(OnIronSourceInitializedCallback);
        }

        private void OnIronSourceInitializedCallback(bool isInitialized) {
            if (isInitialized) {

                IronSource.Agent.validateIntegration();

                IronSourceEvents.onImpressionDataReadyEvent += OnAdRevenuePaidEvent;

                IronSourceSettings settings = IronSourceSettings.Instance;
                bannerAdSize = settings.BannerAdSize;

                if (!IsRemoveAdFormat(Services.Advertisings.AdFormat.InterstitialAd)) {
                    InterstitialAds = new AdCollection<Services.Advertisings.InterstitialAd>();

                    if (settings.InterstitialAdIds != null && settings.InterstitialAdIds.Length > 0) {
                        for (int i = 0; i < settings.InterstitialAdIds.Length; i++) {
                            IronSourceInterstitialAd interstitialAd = new IronSourceInterstitialAd(this, settings.InterstitialAdIds[i]);
                            InterstitialAds.Add(interstitialAd);
                        }
                    }
                }

                
                if (!IsRemoveAdFormat(Services.Advertisings.AdFormat.RewardedAd)) {
                    RewardedAds = new AdCollection<Services.Advertisings.RewardedAd>();

                    if (settings.RewardedAdIds != null && settings.RewardedAdIds.Length > 0) {
                        for (int i = 0; i < settings.RewardedAdIds.Length; i++) {
                            IronSourceRewardedAd rewardedAd = new IronSourceRewardedAd(this, settings.RewardedAdIds[i]);
                            RewardedAds.Add(rewardedAd);
                        }
                    }
                }

                if (!IsRemoveAdFormat(Services.Advertisings.AdFormat.BannerAd)) {
                    BannerAds = new AdCollection<Services.Advertisings.BannerAd>();

                    if (settings.BannerAdIds != null && settings.BannerAdIds.Length > 0) {
                        for (int i = 0; i < settings.BannerAdIds.Length; i++) {
                            IronSourceBannerAd bannerAd = new IronSourceBannerAd(this, settings.BannerAdIds[i], bannerAdSize);
                            BannerAds.Add(bannerAd);
                        }
                    }
                }

                Database.Unload(settings);

                Log.Info("[IronSourceAdvertisings] IronSource advertisings initialize completed.");
                InitializeEvent.Invoke(true);
            } else {
                Log.Error("[IronSourceAdvertisings] IronSource advertisings initialize failed because IronSource initialize failed");
                InitializeEvent.Invoke(false);
            }
        }

        private void OnAdRevenuePaidEvent(IronSourceImpressionData impressionData) {
            HAVIGAME.Services.Advertisings.AdFormat adFormat = GetAdFormat(impressionData.adFormat);

            AdRevenuePaid adImpression = new AdRevenuePaid() {
                adSource = impressionData.adNetwork,
                adNetwork = Network,
                adFormat = adFormat,
                placement = "unknow",
                adUnitId = impressionData.instanceName,
                value = impressionData.revenue.HasValue ? impressionData.revenue.Value : 0,
                currency = "USD",
            };

            InvokeAdRevenuePaidEvent(adFormat, adImpression);
        }

        private HAVIGAME.Services.Advertisings.AdFormat GetAdFormat(string stringValue) {
            switch (stringValue) {
                case "banner": return HAVIGAME.Services.Advertisings.AdFormat.BannerAd;
                case "interstitial": return HAVIGAME.Services.Advertisings.AdFormat.InterstitialAd;
                case "rewarded": return HAVIGAME.Services.Advertisings.AdFormat.RewardedAd;
                default: return HAVIGAME.Services.Advertisings.AdFormat.Unknow;
            }
        }

        private class IronSourceInterstitialAd : Services.Advertisings.InterstitialAd {
            private IronSourceAdvertisings client;
            private string placement;
            private Action onCompleted;
            private int retry;
            private LevelPlayInterstitialAd ad;

            public override AdService Client => client;
            public override bool IsReady {
                get {
                    if (ad != null) {
                        return ad.IsAdReady();
                    }
                    return false;
                }
            }

            public IronSourceInterstitialAd(IronSourceAdvertisings client, AdId id) : base(id) {
                this.client = client;
                this.retry = 0;
                this.placement = "NULL";
                ad = null;

                if (HasAutoLoad(LoadType.OnCreated)) {
                    Load();
                }
            }

            public override Ad Clone() {
                return new IronSourceInterstitialAd(client, AdId.Clone());
            }

            public override bool Load() {
                if (IsReady) {
                    Log.Warning("[IronSourceInterstitialAd] Load failed! Interstitial ad is ready!");
                    return false;
                }

                if (IsReloading) {
                    Log.Warning("[IronSourceInterstitialAd] Load failed! Interstitial ad is reloading!");
                    return false;
                }

                if (IsLoading) {
                    Log.Warning("[IronSourceInterstitialAd] Load failed! Interstitial ad is loading!");
                    return false;
                }

                if (string.IsNullOrEmpty(Id)) {
                    Log.Warning("[IronSourceInterstitialAd] Load failed! Interstitial ad id is null or empty!");
                    return false;
                }

                IsLoading = true;
                InvokeOnLoadEvent(placement);

                if (ad == null) {
                    ad = new LevelPlayInterstitialAd(Id);

                    ad.OnAdLoaded += OnAdLoadedEvent;
                    ad.OnAdLoadFailed += OnAdLoadFailedEvent;
                    ad.OnAdDisplayed += OnAdDisplayed;
                    ad.OnAdDisplayFailed += OnAdDisplayFailed;
                    ad.OnAdClicked += OnAdClicked;
                    ad.OnAdClosed += OnAdClosed;
                    ad.OnAdInfoChanged += OnAdInfoChanged;
                }

                ad.LoadAd();

                return true;
            }

            public override bool Show(Action onCompleted, string placement) {
                if (!IsReady) {
                    Log.Warning("[IronSourceInterstitialAd] Show failed! Interstitial ad not avaliable!");
                    return false;
                }

                if (IsShowing) {
                    Log.Warning("[IronSourceInterstitialAd] Show failed! Interstitial ad is showing!");
                    return false;
                }

                IsShowing = true;
                this.onCompleted = onCompleted;
                this.placement = placement;

                InvokeOnDisplayEvent(placement);

                ad.ShowAd();
                return true;
            }

            public override bool Destroy() {
                if (ad != null) {
                    ad.DestroyAd();
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

            private void OnAdLoadedEvent(LevelPlayAdInfo info) {
                IsLoading = false;

                InvokeOnLoadedEvent(placement);

                retry = 0;
            }

            private void OnAdLoadFailedEvent(LevelPlayAdError error) {
                IsLoading = false;

                InvokeOnLoadFailedEvent(placement, error.ToString());

                if (HasAutoLoad(LoadType.OnLoadFailed)) {
                    retry++;
                    float delay = 2 * Math.Min(6, retry);
                    IsReloading = true;
                    Invoke(Reload, delay);
                }
            }

            private void OnAdDisplayed(LevelPlayAdInfo info) {
                InvokeOnDisplayedEvent(placement);
            }

            private void OnAdDisplayFailed(LevelPlayAdDisplayInfoError error) {
                if (ad != null) {
                    ad.DestroyAd();
                    ad = null;
                }

                IsShowing = false;
                Invoke(onCompleted);
                InvokeOnDisplayFailedEvent(placement, error.ToString());

                if (HasAutoLoad(LoadType.OnDisplayFailed)) {
                    Reload(true);
                }
            }

            private void OnAdClicked(LevelPlayAdInfo info) {
                InvokeOnClickedEvent(placement);
            }

            private void OnAdInfoChanged(LevelPlayAdInfo info) {

            }

            private void OnAdClosed(LevelPlayAdInfo info) {
                if (ad != null) {
                    ad.DestroyAd();
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

        private class IronSourceRewardedAd : Services.Advertisings.RewardedAd {
            private IronSourceAdvertisings client;
            private string placement;
            private Action onCompleted;
            private Action onFailed;
            private int retry;
            private bool receivedReward;
            private LevelPlayRewardedAd ad;

            public override AdService Client => client;
            public override bool IsReady {
                get {
                    if (ad != null) {
                        return ad.IsAdReady();
                    }
                    return false;
                }
            }

            public IronSourceRewardedAd(IronSourceAdvertisings client, AdId id) : base(id) {
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
                return new IronSourceRewardedAd(client, AdId.Clone());
            }

            public override bool Load() {
                if (IsReady) {
                    Log.Warning("[IronSourceRewardedAd] Load failed! Rewarded ad is ready!");
                    return false;
                }

                if (IsReloading) {
                    Log.Warning("[IronSourceRewardedAd] Load failed! Rewarded ad is reloading!");
                    return false;
                }

                if (IsLoading) {
                    Log.Warning("[IronSourceRewardedAd] Load failed! Rewarded ad is loading!");
                    return false;
                }

                if (string.IsNullOrEmpty(Id)) {
                    Log.Warning("[IronSourceRewardedAd] Load failed! Rewarded ad id is null or empty!");
                    return false;
                }

                IsLoading = true;
                InvokeOnLoadEvent(placement);

                if (ad == null) {
                    ad = new LevelPlayRewardedAd(Id);

                    ad.OnAdLoaded += OnAdLoadedEvent;
                    ad.OnAdLoadFailed += OnAdLoadFailedEvent;
                    ad.OnAdDisplayed += OnAdDisplayed;
                    ad.OnAdDisplayFailed += OnAdDisplayFailed;
                    ad.OnAdRewarded += OnUserRewardEarned;
                    ad.OnAdClicked += OnAdClicked;
                    ad.OnAdClosed += OnAdClosed;
                    ad.OnAdInfoChanged += OnAdInfoChanged;
                }

                ad.LoadAd();

                return true;
            }

            public override bool Show(Action onCompleted, Action onFailed, string placement) {
                if (!IsReady) {
                    Log.Warning("[IronSourceRewardedAd] Show failed! Rewarded ad not avaliable!");
                    return false;
                }

                if (IsShowing) {
                    Log.Warning("[IronSourceRewardedAd] Show failed! Rewarded ad is showing!");
                    return false;
                }

                IsShowing = true;
                receivedReward = false;
                this.onCompleted = onCompleted;
                this.placement = placement;

                InvokeOnDisplayEvent(placement);

                ad.ShowAd();
                return true;
            }

            public override bool Destroy() {
                if (ad != null) {
                    ad.DestroyAd();
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

            private void OnAdLoadedEvent(LevelPlayAdInfo info) {
                IsLoading = false;

                InvokeOnLoadedEvent(placement);

                retry = 0;
            }

            private void OnAdLoadFailedEvent(LevelPlayAdError error) {
                IsLoading = false;

                InvokeOnLoadFailedEvent(placement, error.ToString());

                if (HasAutoLoad(LoadType.OnLoadFailed)) {
                    retry++;
                    float delay = 2 * Math.Min(6, retry);
                    IsReloading = true;
                    Invoke(Reload, delay);
                }
            }

            private void OnAdDisplayed(LevelPlayAdInfo info) {
                InvokeOnDisplayedEvent(placement);
            }

            private void OnAdDisplayFailed(LevelPlayAdDisplayInfoError error) {
                if (ad != null) {
                    ad.DestroyAd();
                    ad = null;
                }

                receivedReward = false;
                IsShowing = false;
                Invoke(onCompleted);
                InvokeOnDisplayFailedEvent(placement, error.ToString());

                if (HasAutoLoad(LoadType.OnDisplayFailed)) {
                    Reload(true);
                }
            }

            private void OnAdClicked(LevelPlayAdInfo info) {
                InvokeOnClickedEvent(placement);
            }

            private void OnAdInfoChanged(LevelPlayAdInfo info) {

            }

            private void OnUserRewardEarned(LevelPlayAdInfo info, LevelPlayReward reward) {
                receivedReward = true;
                CheckReceiveRewardProcess();
            }

            private void OnAdClosed(LevelPlayAdInfo info) {
                if (ad != null) {
                    ad.DestroyAd();
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

        private class IronSourceBannerAd : BannerAd {
            private IronSourceAdvertisings client;
            private string placement;
            private LevelPlayBannerAd bannerView;

            public override AdService Client => client;
            public override bool IsReady {
                get {
                    return true;
                }
            }
            public override float Width {
                get {
                    if (IsShowing) {
                        return IronSourceExtensions.ConvertDpToPixels(bannerView.GetAdSize().Width);
                    }
                    return 0;
                }
            }
            public override float Height {
                get {
                    if (IsShowing) {
                        return IronSourceExtensions.ConvertDpToPixels(bannerView.GetAdSize().Height);
                    }
                    return 0;
                }
            }

            public IronSourceBannerAd(IronSourceAdvertisings client, AdId id, AdSizeOption size) : base(id, true, size) {
                this.client = client;
                this.placement = "NULL";
                this.bannerView = null;

                if (HasAutoLoad(LoadType.OnCreated)) {
                    Load();
                }
            }

            public override Ad Clone() {
                IronSourceBannerAd ad = new IronSourceBannerAd(client, AdId.Clone(), Size);
                return ad;
            }

            public override bool Load() {
                return InternalCreate(Position);
            }

            private bool InternalCreate(AdPositions position) {

                if (Created) {
                    Log.Warning("[IronSourceBannerAd] Banner ad has created!");
                    return false;
                }

                if (string.IsNullOrEmpty(Id)) {
                    Log.Warning("[IronSourceBannerAd] Create ad failed! Banner ad id is null or empty!");
                    return false;
                }

                Created = true;
                Position = position;

                bannerView = new LevelPlayBannerAd(Id, Size.ToAdSize(), IronSourceExtensions.ToAdPosition(position), null, false, false);
                bannerView.ResumeAutoRefresh();

                bannerView.OnAdLoaded += OnAdLoadedEvent;
                bannerView.OnAdLoadFailed += OnAdLoadFailedEvent;
                bannerView.OnAdDisplayed += OnAdDisplayed;
                bannerView.OnAdDisplayFailed += OnAdDisplayFailed;
                bannerView.OnAdCollapsed += OnAdCollapsed;
                bannerView.OnAdExpanded += OnAdExpanded;
                bannerView.OnAdLeftApplication += OnAdLeftApplication;
                bannerView.OnAdClicked += OnAdClicked;

                bannerView.LoadAd();

                return true;
            }

            public override bool Show(AdPositions position, Vector2Int offset, string placement) {
                if (!Created) {
                    Log.Warning("[IronSourceBannerAd] Banner ad not been created! Create a new banner ad.");
                    if (!InternalCreate(position)) {
                        return false;
                    }
                }

                if (Position != position) {
                    Destroy();
                    InternalCreate(position);
                }

                IsShowing = true;
                IsLoading = true;
                this.Position = position;
                this.placement = placement;

                InvokeOnDisplayEvent(placement);

                bannerView.ShowAd();

                InvokeOnDisplayedEvent(placement);

                return true;
            }

            public override void SetPosition(AdPositions position, Vector2Int offset) {
                Show(position, offset, placement);
            }

            public override bool Hide() {
                if (!IsShowing) {
                    Log.Warning("[IronSourceBannerAd] Banner ad not showing!");
                    return false;
                }

                IsShowing = false;
                bannerView.HideAd();

                InvokeOnClosedEvent(placement);

                return true;
            }

            public override bool Destroy() {
                if (!Created) {
                    Log.Warning("[IronSourceBannerAd] Banner ad not been created!");
                    return false;
                }

                Created = false;

                if (IsShowing) {
                    IsShowing = false;

                    InvokeOnClosedEvent(placement);
                }

                bannerView.DestroyAd();
                bannerView = null;
                this.Position = AdPositions.Null;
                this.Offset = Vector2Int.zero;

                return true;
            }

            private void OnAdLoadedEvent(LevelPlayAdInfo info) {
                IsLoading = false;

                if (IsShowing) {
                    bannerView.ShowAd();
                }

                InvokeOnLoadedEvent(placement);
            }

            private void OnAdLoadFailedEvent(LevelPlayAdError error) {
                IsLoading = false;

                InvokeOnLoadFailedEvent(placement, error.ToString());
            }

            private void OnAdDisplayed(LevelPlayAdInfo info) {
                InvokeOnDisplayedEvent(placement);
            }

            private void OnAdDisplayFailed(LevelPlayAdDisplayInfoError error) {
                InvokeOnDisplayFailedEvent(placement, error.ToString());
            }

            private void OnAdClicked(LevelPlayAdInfo info) {
                InvokeOnClickedEvent(placement);
            }

            private void OnAdCollapsed(LevelPlayAdInfo info) {

            }

            private void OnAdExpanded(LevelPlayAdInfo info) {

            }

            private void OnAdLeftApplication(LevelPlayAdInfo info) {

            }
        }
    }
#endif

#if IRON_SOURCE
    public static class IronSourceExtensions {
        public static float ConvertDpToPixels(float dp) {
            return dp * (Screen.dpi / 160f);
        }

        public static float ConverPixelsToDp(float pixel) {
            return pixel / (Screen.dpi / 160f);
        }

        public static LevelPlayBannerPosition ToAdPosition(this AdPositions position) {
            switch (position) {
                case AdPositions.TopCenter:
                    return LevelPlayBannerPosition.TopCenter;
                case AdPositions.TopLeft:
                    return LevelPlayBannerPosition.TopCenter;
                case AdPositions.TopRight:
                    return LevelPlayBannerPosition.TopCenter;
                case AdPositions.Centered:
                    return LevelPlayBannerPosition.TopCenter;
                case AdPositions.CenterLeft:
                    return LevelPlayBannerPosition.TopCenter;
                case AdPositions.CenterRight:
                    return LevelPlayBannerPosition.TopCenter;
                case AdPositions.BottomCenter:
                    return LevelPlayBannerPosition.BottomCenter;
                case AdPositions.BottomLeft:
                    return LevelPlayBannerPosition.BottomCenter;
                case AdPositions.BottomRight:
                    return LevelPlayBannerPosition.BottomCenter;
                default:
                    return LevelPlayBannerPosition.BottomCenter;
            }
        }

        public static LevelPlayAdSize ToAdSize(this AdSizeOption adSize) {
            switch (adSize.Type) {
                case AdSizeType.Banner: return LevelPlayAdSize.BANNER;
                case AdSizeType.MediumRectangle: return LevelPlayAdSize.MEDIUM_RECTANGLE;
                case AdSizeType.IABBanner: return LevelPlayAdSize.LARGE;
                case AdSizeType.Leaderboard: return LevelPlayAdSize.LEADERBOARD;
                case AdSizeType.AnchoredAdaptive: return LevelPlayAdSize.CreateAdaptiveAdSize();
                default: return LevelPlayAdSize.CreateCustomBannerSize(adSize.Width, adSize.Height);
            }
        }
    }
#endif

    [CategoryMenu("IronSource Advertisings")]
    [System.Serializable]
    public class IronSourceAdServiceProvider : AdServiceProvider {
        public override AdService GetService() {
#if IRON_SOURCE
            return new IronSourceAdvertisings();
#else
            return null;
#endif
        }
    }
}
