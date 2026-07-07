using HAVIGAME.Services.Advertisings;
using UnityEngine;

namespace HAVIGAME.Plugins.IronSources {

    [DefineSymbols(IronSourceManager.DEFINE_SYMBOL)]
    [SettingMenu(typeof(IronSourceSettings), "Plugins/IronSource", "", "https://developers.is.com/ironsource-mobile/unity/levelplay-starter-kit/", 201, "Icons/icon_ironsource.psd")]
    [CreateAssetMenu(fileName = "IronSourceSettings", menuName = "HAVIGAME/Settings/Plugins/IronSource")]
    public class IronSourceSettings : Settings<IronSourceSettings> {
        [SerializeField] private BoolProperty enable = BoolProperty.Create();
        [SerializeField] private string appId;
        [SerializeReference, Subclass] private AdId[] rewardedAdIds;
        [SerializeReference, Subclass] private AdId[] interstitialAdIds;
        [SerializeReference, Subclass] private AdId[] bannerAdIds;
        [SerializeField] private AdSizeOption bannerAdSize = new AdSizeOption(AdSizeType.AnchoredAdaptive);

        public bool Enable => enable.Get();
        public string AppID => appId;
        public AdId[] RewardedAdIds => rewardedAdIds;
        public AdId[] InterstitialAdIds => interstitialAdIds;
        public AdId[] BannerAdIds => bannerAdIds;
        public AdSizeOption BannerAdSize => bannerAdSize;
    }
}
