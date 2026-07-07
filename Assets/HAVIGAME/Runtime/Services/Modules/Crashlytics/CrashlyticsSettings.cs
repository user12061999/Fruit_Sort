using UnityEngine;

namespace HAVIGAME.Services.Crashlytics {

    [DefineSymbols(CrashlyticManager.DEFINE_SYMBOL)]
    [SettingMenu(typeof(CrashlyticsSettings), "Services/Crashlytics", "", null, 101, "Icons/icon_crashlytics.psd")]
    [CreateAssetMenu(fileName = "CrashlyticsSettings", menuName = "HAVIGAME/Settings/Services/Crashlytics")]
    public class CrashlyticsSettings : Settings<CrashlyticsSettings> {

        [SerializeReference, Subclass] private CrashlyticServiceProvider[] serviceProviders;

        public CrashlyticServiceProvider[] GetServiceProviders() {
            return serviceProviders;
        }
    }
}
