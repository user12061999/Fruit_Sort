using UnityEngine;

namespace HAVIGAME.Services.Analytics {

    [DefineSymbols(AnalyticManager.DEFINE_SYMBOL)]
    [SettingMenu(typeof(AnalyticsSettings), "Services/Analytics", "", null, 101, "Icons/icon_analytics.psd")]
    [CreateAssetMenu(fileName = "AnalyticsSettings", menuName = "HAVIGAME/Settings/Services/Analytics")]
    public class AnalyticsSettings : Settings<AnalyticsSettings> {

        [SerializeReference, Subclass] private AnalyticServiceProvider[] serviceProviders;

        public AnalyticServiceProvider[] GetServiceProviders() {
            return serviceProviders;
        }
    }
}
