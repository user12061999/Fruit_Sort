using UnityEngine;

namespace HAVIGAME.Plugins.AppsFlyer {

    [DefineSymbols(AppsFlyerManager.DEFINE_SYMBOL)]
    [SettingMenu(typeof(AppsFlyerSettings), "Plugins/AppsFlyer", "", "https://support.appsflyer.com/hc/vi/articles/360007314277", 201, "Icons/icon_appsflyer.psd")]
    [CreateAssetMenu(fileName = "AppsFlyerSettings", menuName = "HAVIGAME/Settings/Plugins/AppsFlyer")]
    public class AppsFlyerSettings : Settings<AppsFlyerSettings> {
        [SerializeField] private string devKey;
        [SerializeField] private string appID;
        [SerializeField] private bool saveConversionData = true;
        [SerializeField] private string saveId = "appsflyer";
        [SerializeField] private bool sanboxMode = false;

        public string DevKey => devKey;
        public string AppID => appID;
        public bool SaveConversionData => saveConversionData;
        public string SaveId => saveId;
        public bool SanboxMode => sanboxMode; 
    }
}
