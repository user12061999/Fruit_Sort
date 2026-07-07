using UnityEngine;
using HAVIGAME.Services.Advertisings;

namespace HAVIGAME.Plugins.AdMob {

    [DefineSymbols(AdMobManager.DEFINE_SYMBOL)]
    [SettingMenu(typeof(AdMobSettings), "Plugins/AdMob", "", "https://developers.google.com/admob/unity/quick-start", 201, "Icons/icon_admob.psd")]
    [CreateAssetMenu(fileName = "AdMobSettings", menuName = "HAVIGAME/Settings/Plugins/AdMob")]
    public class AdMobSettings : Settings<AdMobSettings> {
        [SerializeField] private BoolProperty enable = BoolProperty.Create();
        [SerializeReference, Subclass] private AdId[] appOpenAdIds;
        [SerializeReference, Subclass] private AdId[] rewardedAdIds;
        [SerializeReference, Subclass] private AdId[] interstitialAdIds;
        [SerializeReference, Subclass] private AdId[] bannerAdIds;
        [SerializeReference, Subclass] private AdId[] mediumRectangleAdIds;
        [SerializeReference, Subclass] private AdId[] rewardedInterstitialAdIds;
        [SerializeReference, Subclass] private AdId[] nativeAdIds;
        [SerializeReference, Subclass] private AdId[] nativeOverlayAdIds;
        [SerializeReference, Subclass] private AdId[] nativeImmersiveAdIds;
        [SerializeField] private Result tagForChildDirectedTreatment = Result.Unspecified;
        [SerializeField] private Result tagForUnderAgeOfConsent = Result.Unspecified;
        [Header("[Options]")]
        [SerializeField] private AdSizeOption bannerAdSize = new AdSizeOption(AdSizeType.AnchoredAdaptive);
        [SerializeField, DefineCondition("ADMOB_NATIVE")] private bool nativeAdEnabled;
        [SerializeField, DefineCondition("ADMOB_NATIVE_IMMERSIVE")] private bool nativeImmersiveAdEnabled;
        [SerializeField, DefineCondition("ADMOB_CUSTOM")] private bool customAdEnabled;
        [SerializeField] private BoolProperty collapsibleBanner = BoolProperty.Create();
        [SerializeField] private IntProperty preloadBufferSize = IntProperty.Create();

        public bool Enable => enable.Get();
        public AdId[] AppOpenAdIds => appOpenAdIds;
        public AdId[] RewardedAdIds => rewardedAdIds;
        public AdId[] InterstitialAdIds => interstitialAdIds;
        public AdId[] BannerAdIds => bannerAdIds;
        public AdId[] MediumRectangleAdIds => mediumRectangleAdIds;
        public AdId[] RewardedInterstitialAdIds => rewardedInterstitialAdIds;
        public AdId[] NativeAdIds => nativeAdIds;
        public AdId[] NativeOverlayAdIds => nativeOverlayAdIds;
        public AdId[] NativeImmersiveAdIds => nativeImmersiveAdIds;
        public Result TagForChildDirectedTreatment => tagForChildDirectedTreatment;
        public Result TagForUnderAgeOfConsent => tagForUnderAgeOfConsent;
        public AdSizeOption BannerAdSize => bannerAdSize;
        public bool CollapsibleBanner => collapsibleBanner.Get();
        public int PreloadBufferSize => preloadBufferSize.Get();
    }
}
