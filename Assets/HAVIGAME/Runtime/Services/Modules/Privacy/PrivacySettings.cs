using UnityEngine;

namespace HAVIGAME.Services.Privacy {

    [DefineSymbols(PrivacyManager.DEFINE_SYMBOL)]
    [SettingMenu(typeof(PrivacySettings), "Services/Privacy", "", null, 101, "Icons/icon_privacy.psd")]
    [CreateAssetMenu(fileName = "PrivacySettings", menuName = "HAVIGAME/Settings/Services/Privacy")]
    public class PrivacySettings : Settings<PrivacySettings> {

        [SerializeReference, Subclass] private PrivacyServiceProvider serviceProvider;

        public PrivacyServiceProvider ServiceProvider => serviceProvider;
    }
}
