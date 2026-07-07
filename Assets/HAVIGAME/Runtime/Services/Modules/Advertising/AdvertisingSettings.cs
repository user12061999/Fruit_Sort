using UnityEngine;

namespace HAVIGAME.Services.Advertisings {

    [DefineSymbols(AdvertisingManager.DEFINE_SYMBOL)]
    [SettingMenu(typeof(AdvertisingSettings), "Services/Advertisings", "", null, 101, "Icons/icon_ad.psd")]
    [CreateAssetMenu(fileName = "AdvertisingSettings", menuName = "HAVIGAME/Settings/Services/Advertisings")]
    public class AdvertisingSettings : Settings<AdvertisingSettings> {

        [SerializeReference, Subclass] private AdServiceProvider[] serviceProviders; 
        [SerializeField] private bool forceRunOnMainTheard = true;
        [SerializeField] private bool saveData = true;
        [SerializeField] private string saveId = "advertising";
        [SerializeField] private AdFormat removeAdFormats = AdFormat.InterstitialAd | AdFormat.BannerAd | AdFormat.AppOpenAd;

        public bool SaveData => saveData;
        public string SaveId => saveId;
        public bool ForceRunOnMainTheard => forceRunOnMainTheard;
        public AdFormat RemoveAdFormats => removeAdFormats;

        public AdServiceProvider[] GetServiceProviders() {
            return serviceProviders;
        }
    }
}
