namespace HAVIGAME.Services.Advertisings {
    public abstract class Ad {
        public static int nextUniqueId = 1;

        private int uniqueId;
        private object owner;
        private AdId adId;
        private LoadType autoLoadType;
        private bool isShowing;
        private bool isLoading;
        private bool isReloading;

        public bool Owned => owner != null;
        public AdId AdId => adId;
        public int UniqueId => uniqueId;
        public string Id => adId.Current();
        public abstract AdService Client { get; }
        public abstract AdFormat Format { get; }
        public abstract bool IsReady { get; }
        public bool IsShowing {
            get {
                return isShowing;
            }
            protected set {
                if (isShowing != value) {
                    isShowing = value;
                }
            }
        }
        public bool IsLoading {
            get {
                return isLoading;
            }
            protected set {
                if (isLoading != value) {
                    isLoading = value;
                }
            }
        }
        public bool IsReloading {
            get {
                return isReloading;
            }
            protected set {
                if (isReloading != value) {
                    isReloading = value;
                }
            }
        }
        public AdGroup Group => adId.Group;
        public LoadType AutoLoadType => autoLoadType;

        public Ad(AdId adId) {
            this.owner = null;
            this.adId = adId;
            this.adId.Reset();
            this.autoLoadType = adId.AutoLoadType;
            this.isShowing = false;
            this.isLoading = false;
            this.isReloading = false;
            this.uniqueId = nextUniqueId;

            nextUniqueId++;

            Log.Debug(Utility.Text.Format("[Ad] Create ad, format = {0}, id = {1}, unique id = {2}.", Format, Id, UniqueId));
        }

        public abstract Ad Clone();
        public abstract bool Load();
        public abstract bool Destroy();

        public bool IsOwned(object target) {
            return owner != null && owner == target;
        }
        public void SetOwner(object owner) {
            this.owner = owner;
        }
        public void SetAutoLoad(LoadType autoLoadType) {
            this.autoLoadType = autoLoadType;
        }
        public bool IsInGroup(AdGroup group) {
            return group.HasFlag(Group);
        }
        public bool HasAutoLoad(LoadType type) {
            return autoLoadType.HasFlag(type);
        }

        protected void InvokeOnRevenuePaidEvent(AdRevenuePaid args) {
            Client.InvokeAdRevenuePaidEvent(Format, args);
        }
        protected void InvokeOnLoadEvent(string placement) {
            AdEventArgs args = ReferencePool.Acquire<AdEventArgs>();
            args.SetParam(Client.Network, Format, Id, UniqueId, placement);
            Client.InvokeOnAdLoadEvent(args);
        }
        protected void InvokeOnLoadedEvent(string placement) {
            AdEventArgs args = ReferencePool.Acquire<AdEventArgs>();
            args.SetParam(Client.Network, Format, Id, UniqueId, placement);
            Client.InvokeOnAdLoadedEvent(args);
        }
        protected void InvokeOnLoadFailedEvent(string placement, string error) {
            AdEventArgs args = ReferencePool.Acquire<AdEventArgs>();
            args.SetParam(Client.Network, Format, Id, UniqueId, placement, error);
            Client.InvokeOnAdLoadFailedEvent(args);
        }
        protected void InvokeOnDisplayEvent(string placement) {
            AdEventArgs args = ReferencePool.Acquire<AdEventArgs>();
            args.SetParam(Client.Network, Format, Id, UniqueId, placement);
            Client.InvokeOnAdDisplayEvent(args);
        }
        protected void InvokeOnDisplayedEvent(string placement) {
            AdEventArgs args = ReferencePool.Acquire<AdEventArgs>();
            args.SetParam(Client.Network, Format, Id, UniqueId, placement);
            Client.InvokeOnAdDisplayedEvent(args);
        }
        protected void InvokeOnDisplayFailedEvent(string placement, string error) {
            AdEventArgs args = ReferencePool.Acquire<AdEventArgs>();
            args.SetParam(Client.Network, Format, Id, UniqueId, placement, error);
            Client.InvokeOnAdDisplayFailedEvent(args);
        }
        protected void InvokeOnClickedEvent(string placement) {
            AdEventArgs args = ReferencePool.Acquire<AdEventArgs>();
            args.SetParam(Client.Network, Format, Id, UniqueId, placement);
            Client.InvokeOnAdClickedEvent(args);
        }
        protected void InvokeOnClosedEvent(string placement) {
            AdEventArgs args = ReferencePool.Acquire<AdEventArgs>();
            args.SetParam(Client.Network, Format, Id, UniqueId, placement);
            Client.InvokeOnAdClosedEvent(args);
        }

        protected void Invoke(System.Action action) {
            Executor.Instance.RunOnMainTheard(action);
        }
        protected void Invoke(System.Action action, float delay) {
            Executor.Instance.RunOnMainTheard(action, delay);
        }

        public override string ToString() {
            return string.Format("format = {0}, id = {1}, unique id = {2}", Format, Id, UniqueId);
        }
    }

    [System.Flags]
    [System.Serializable]
    public enum AdFormat {
        Unknow = 0,
        AppOpenAd = 1 << 0,
        BannerAd = 1 << 1,
        RewardedAd = 1 << 2,
        InterstitialAd = 1 << 3,
        RewardedInterstitialAd = 1 << 4,
        MediumRectangleAd = 1 << 5,
        NativeAd = 1 << 6,
        NativeOverlayAd = 1 << 7,
        NativeImmersiveAd = 1 << 8,

        AdMobAdUnits = AppOpenAd | BannerAd | RewardedAd | InterstitialAd | MediumRectangleAd | RewardedInterstitialAd | NativeOverlayAd,
        AppLovinAdUnits = AppOpenAd | BannerAd | RewardedAd | InterstitialAd | MediumRectangleAd,
        IronSourceAdUnits = BannerAd | RewardedAd | InterstitialAd,
        All = AppOpenAd | BannerAd | RewardedAd | InterstitialAd | MediumRectangleAd | RewardedInterstitialAd | NativeAd | NativeOverlayAd | NativeImmersiveAd,
    }

    [System.Flags]
    [System.Serializable]
    public enum LoadType {
        None = 0,
        OnCreated = 1 << 0,
        OnLoadFailed = 1 << 1,
        OnExpried = 1 << 2,
        OnDisplayFailed = 1 << 3,
        OnClosed = 1 << 4,
        OnDestroyed = 1 << 5,
        MediationPreload = 1 << 6,
        Custom_1 = 1 << 23,
        Custom_2 = 1 << 24,
        Custom_3 = 1 << 25,
        Custom_4 = 1 << 26,
        Custom_5 = 1 << 27,
        Custom_6 = 1 << 28,
        Custom_7 = 1 << 29,
        Custom_8 = 1 << 30,
        Custom_9 = 1 << 31,
        All = OnCreated | OnLoadFailed | OnExpried | OnDisplayFailed | OnClosed | OnDestroyed,
    }
}
