using System;
using System.Collections.Generic;
using UnityEngine;

namespace HAVIGAME.Services.Advertisings {
    public abstract class AdService {
        public static bool IsRemoveAds => AdvertisingManager.IsRemoveAds;
        public static AdFormat RemoveAdFormats => AdvertisingManager.RemoveAdFormats;
        public static bool IsRemoveAdFormat(AdFormat adFormat) => IsRemoveAds && RemoveAdFormats.HasFlag(adFormat);

        public readonly InitializeEvent initializeEvent = new InitializeEvent();

        public InitializeEvent InitializeEvent => initializeEvent;
        public bool IsInitialized => InitializeEvent.IsInitialized;

        public abstract HAVIGAME.Services.Advertisings.AdNetwork Network { get; }
        public abstract HAVIGAME.Services.Advertisings.AdFormat AdFormatSupported { get; }
        public abstract void Initialize();

        public bool IsInNetwork(AdNetwork network) {
            return network.HasFlag(Network);
        }

        public bool IsAdSupported(AdFormat format) {
            return AdFormatSupported.HasFlag(format);
        }

        #region Events
        internal event AdClientRevenuePaidDelegate onAdRevenuePaid;

        internal AdDelegate onAdLoad;
        internal AdDelegate onAdLoaded;
        internal AdDelegate onAdLoadFailed;
        internal AdDelegate onAdDisplay;
        internal AdDelegate onAdDisplayed;
        internal AdDelegate onAdDisplayFailed;
        internal AdDelegate onAdClicked;
        internal AdDelegate onAdClosed;

        internal void InvokeAdRevenuePaidEvent(AdFormat adFormat, AdRevenuePaid adImpression) {
            onAdRevenuePaid?.Invoke(this, adFormat, adImpression);
        }

        internal void InvokeOnAdLoadEvent(AdEventArgs args) {
            onAdLoad?.Invoke(args);
        }
        internal void InvokeOnAdLoadedEvent(AdEventArgs args) {
            onAdLoaded?.Invoke(args);
        }
        internal void InvokeOnAdLoadFailedEvent(AdEventArgs args) {
            onAdLoadFailed?.Invoke(args);
        }
        internal void InvokeOnAdDisplayEvent(AdEventArgs args) {
            onAdDisplay?.Invoke(args);
        }
        internal void InvokeOnAdDisplayedEvent(AdEventArgs args) {
            onAdDisplayed?.Invoke(args);
        }
        internal void InvokeOnAdDisplayFailedEvent(AdEventArgs args) {
            onAdDisplayFailed?.Invoke(args);
        }
        internal void InvokeOnAdClickedEvent(AdEventArgs args) {
            onAdClicked?.Invoke(args);
        }
        internal void InvokeOnAdClosedEvent(AdEventArgs args) {
            onAdClosed?.Invoke(args);
        }
        #endregion

        public AdCollection<AppOpenAd> AppOpenAds { get; protected set; }
        public AdCollection<InterstitialAd> InterstitialAds { get; protected set; }
        public AdCollection<RewardedAd> RewardedAds { get; protected set; }
        public AdCollection<BannerAd> BannerAds { get; protected set; }
        public AdCollection<RewardedInterstitialAd> RewardedInterstitialAds { get; protected set; }
        public AdCollection<MediumRectangleAd> MediumRectangleAds { get; protected set; }
        public AdCollection<NativeAd> NativeAds { get; protected set; }
        public AdCollection<NativeOverlayAd> NativeOverlayAds { get; protected set; }
        public AdCollection<NativeImmersiveAd> NativeImmersiveAds { get; protected set; }

        public bool IsAppOpenAdInitialized => AppOpenAds != null;
        public bool IsInterstitialAdInitialized => InterstitialAds != null;
        public bool IsRewardedAdInitialized => RewardedAds != null;
        public bool IsBannerAdInitialized => BannerAds != null;
        public bool IsRewardedInterstitialAdInitialized => RewardedInterstitialAds != null;
        public bool IsMediumRectangleAdInitialized => MediumRectangleAds != null;
        public bool IsNativeAdInitialized => NativeAds != null;
        public bool IsNativeOverlayAdInitialized => NativeOverlayAds != null;
        public bool IsNativeImmersiveAdInitialized => NativeImmersiveAds != null;

        public Ad Add(AdFormat format, AdId id) {
            switch (format) {
                case AdFormat.AppOpenAd:
                    if (IsAppOpenAdInitialized) return CreateAppOpenAd(id);
                    break;
                case AdFormat.BannerAd:
                    if (IsBannerAdInitialized) return CreateBannerAd(id);
                    break;
                case AdFormat.RewardedAd:
                    if (IsRewardedAdInitialized) return CreateRewardedAd(id);
                    break;
                case AdFormat.InterstitialAd:
                    if (IsInterstitialAdInitialized) return CreateInterstitialAd(id);
                    break;
                case AdFormat.RewardedInterstitialAd:
                    if (IsRewardedInterstitialAdInitialized) return CreateRewardedInterstitialAd(id);
                    break;
                case AdFormat.MediumRectangleAd:
                    if (IsMediumRectangleAdInitialized) return CreateMediumRectangleAd(id);
                    break;
                case AdFormat.NativeAd:
                    if (IsNativeAdInitialized) return CreateNativeAd(id);
                    break;
                case AdFormat.NativeOverlayAd:
                    if (IsNativeOverlayAdInitialized) return CreateNativeOverlayAd(id);
                    break;
                case AdFormat.NativeImmersiveAd:
                    if (IsNativeImmersiveAdInitialized) return CreateNativeImmersiveAd(id);
                    break;
            }

            return null;
        }
        public bool Remove(Ad ad) {
            switch (ad.Format) {
                case AdFormat.AppOpenAd:
                    if (IsAppOpenAdInitialized) return AppOpenAds.Remove(ad as AppOpenAd);
                    break;
                case AdFormat.BannerAd:
                    if (IsBannerAdInitialized) return BannerAds.Remove(ad as BannerAd);
                    break;
                case AdFormat.RewardedAd:
                    if (IsRewardedAdInitialized) return RewardedAds.Remove(ad as RewardedAd);
                    break;
                case AdFormat.InterstitialAd:
                    if (IsInterstitialAdInitialized) return InterstitialAds.Remove(ad as InterstitialAd);
                    break;
                case AdFormat.RewardedInterstitialAd:
                    if (IsRewardedInterstitialAdInitialized) return RewardedInterstitialAds.Remove(ad as RewardedInterstitialAd);
                    break;
                case AdFormat.MediumRectangleAd:
                    if (IsMediumRectangleAdInitialized) return MediumRectangleAds.Remove(ad as MediumRectangleAd);
                    break;
                case AdFormat.NativeAd:
                    if (IsNativeAdInitialized) return NativeAds.Remove(ad as NativeAd);
                    break;
                case AdFormat.NativeOverlayAd:
                    if (IsNativeOverlayAdInitialized) return NativeOverlayAds.Remove(ad as NativeOverlayAd);
                    break;
                case AdFormat.NativeImmersiveAd:
                    if (IsNativeImmersiveAdInitialized) return NativeImmersiveAds.Remove(ad as NativeImmersiveAd);
                    break;
            }

            return false;
        }

        protected virtual AppOpenAd CreateAppOpenAd(AdId id) { return null; }
        protected virtual InterstitialAd CreateInterstitialAd(AdId id) { return null; }
        protected virtual RewardedAd CreateRewardedAd(AdId id) { return null; }
        protected virtual BannerAd CreateBannerAd(AdId id) { return null; }
        protected virtual RewardedInterstitialAd CreateRewardedInterstitialAd(AdId id) { return null; }
        protected virtual MediumRectangleAd CreateMediumRectangleAd(AdId id) { return null; }
        protected virtual NativeAd CreateNativeAd(AdId id) { return null; }
        protected virtual NativeOverlayAd CreateNativeOverlayAd(AdId id) { return null; }
        protected virtual NativeImmersiveAd CreateNativeImmersiveAd(AdId id) { return null; }
    }

    public class AdCollection<T> where T : Ad {
        [SerializeField] protected List<T> items;

        public AdCollection() {
            items = new List<T>();
        }

        public T this[int index] {
            get {
                return items[index];
            }
        }

        public int Count => items.Count;

        public IEnumerator<T> GetEnumerator() {
            return items.GetEnumerator();
        }

        public void Add(T item) {
            items.Add(item);
        }

        public bool Remove(T item) {
            return items.Remove(item);
        }

        public void RemoveAt(int index) {
            items.RemoveAt(index);
        }

        public int IndexOf(T data) {
            return items.IndexOf(data);
        }

        public T Get(Func<T, bool> condition) {
            foreach (var item in items) {
                if (condition.Invoke(item)) return item;
            }
            return null;
        }

        public IEnumerable<T> GetAll(Func<T, bool> condition) {
            foreach (var item in items) {
                if (condition.Invoke(item)) yield return item;
            }
        }
    }

    public static class AdCollectionExtensions {
        public static bool TryGet<T>(this AdCollection<T> collection, AdGroup group, out T result) where T : Ad {
            result = collection.Get(ad => ad.IsInGroup(group));
            return result != null;
        }

        public static bool TryGet<T>(this AdCollection<T> collection, AdGroup group, object owner, out T result) where T : Ad {
            result = collection.Get(ad => ad.IsInGroup(group) && ad.IsOwned(owner));
            return result != null;
        }

        public static T Get<T>(this AdCollection<T> collection, AdGroup group) where T : Ad {
            return collection.Get(ad => ad.IsInGroup(group)); ;
        }

        public static T Get<T>(this AdCollection<T> collection, AdGroup group, object owner) where T : Ad {
            return collection.Get(ad => ad.IsInGroup(group) && ad.IsOwned(owner));
        }

        public static IEnumerable<T> GetAll<T>(this AdCollection<T> collection, AdGroup group) where T : Ad {
            return collection.GetAll(ad => ad.IsInGroup(group));
        }

        public static IEnumerable<T> GetAll<T>(this AdCollection<T> collection, AdGroup group, object owner) where T : Ad {
            return collection.GetAll(ad => ad.IsInGroup(group) && ad.IsOwned(owner));
        }

        public static bool TryGetReady<T>(this AdCollection<T> collection, AdGroup group, out T result) where T : Ad {
            result = collection.Get(ad => ad.IsInGroup(group) && ad.IsReady);
            return result != null;
        }

        public static bool TryGetReady<T>(this AdCollection<T> collection, AdGroup group, object owner, out T result) where T : Ad {
            result = collection.Get(ad => ad.IsInGroup(group) && ad.IsReady && ad.IsOwned(owner));
            return result != null;
        }

        public static bool IsReady<T>(this AdCollection<T> collection, AdGroup group) where T : Ad {
            foreach (var ad in collection) {
                if (ad.IsInGroup(group)) {
                    if (ad.IsReady) return true;
                }
            }
            return false;
        }

        public static bool IsReady<T>(this AdCollection<T> collection, AdGroup group, object owner) where T : Ad {
            foreach (var ad in collection) {
                if (ad.IsInGroup(group) && ad.IsOwned(owner)) {
                    if (ad.IsReady) return true;
                }
            }
            return false;
        }

        public static bool IsLoading<T>(this AdCollection<T> collection, AdGroup group) where T : Ad {
            foreach (var ad in collection) {
                if (ad.IsInGroup(group)) {
                    if (ad.IsLoading) return true;
                }
            }
            return false;
        }

        public static bool IsLoading<T>(this AdCollection<T> collection, AdGroup group, object owner) where T : Ad {
            foreach (var ad in collection) {
                if (ad.IsInGroup(group) && ad.IsOwned(owner)) {
                    if (ad.IsLoading) return true;
                }
            }
            return false;
        }

        public static bool TryGetShowing<T>(this AdCollection<T> collection, AdGroup group, out T result) where T : Ad {
            result = collection.Get(ad => ad.IsInGroup(group) && ad.IsShowing);
            return result != null;
        }

        public static bool TryGetShowing<T>(this AdCollection<T> collection, AdGroup group, object owner, out T result) where T : Ad {
            result = collection.Get(ad => ad.IsInGroup(group) && ad.IsShowing && ad.IsOwned(owner));
            return result != null;
        }

        public static bool IsShowing<T>(this AdCollection<T> collection, AdGroup group) where T : Ad {
            foreach (var ad in collection) {
                if (ad.IsInGroup(group)) {
                    if (ad.IsShowing) return true;
                }
            }
            return false;
        }

        public static bool IsShowing<T>(this AdCollection<T> collection, AdGroup group, object owner) where T : Ad {
            foreach (var ad in collection) {
                if (ad.IsInGroup(group) && ad.IsOwned(owner)) {
                    if (ad.IsShowing) return true;
                }
            }
            return false;
        }
    }

    [System.Serializable]
    public abstract class AdServiceProvider : ServiceProvider<AdService> {

    }

    [System.Flags]
    public enum AdNetwork : byte {
        None = 0,
        AdMob = 1 << 0,
        AppLovin = 1 << 1,
        IronSource = 1 << 2,
        All = AdMob | AppLovin | IronSource,
    }

    public struct AdRevenuePaid {
        public string adSource;
        public AdNetwork adNetwork;
        public AdFormat adFormat;
        public string adUnitId;
        public string placement;
        public double value;
        public string currency;

        public override string ToString() {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(adSource.ToString());
            sb.Append($"\n ad_network: {adNetwork}");
            sb.Append($"\n ad_format: {adFormat}");
            sb.Append($"\n ad_unit_id: {adUnitId}");
            sb.Append($"\n placement: {placement}");
            sb.Append($"\n value: {value}");
            sb.Append($"\n currency: {currency}");
            return sb.ToString();
        }
    }
}
