using UnityEngine;

namespace HAVIGAME.Services.RemoteConfig {

    [DefineSymbols(RemoteConfigManager.DEFINE_SYMBOL)]
    [SettingMenu(typeof(RemoteConfigSettings), "Services/Remote Config", "", null, 101, "Icons/icon_remoteconfig.psd")]
    [CreateAssetMenu(fileName = "RemoteConfigSettings", menuName = "HAVIGAME/Settings/Services/Remote Config")]
    public class RemoteConfigSettings : Settings<RemoteConfigSettings> {

        [SerializeReference, Subclass] private RemoteConfigServiceProvider serviceProvider;
        [SerializeField] private bool saveData = true;
        [SerializeField] private string saveId = "remote_config";

        public RemoteConfigServiceProvider ServiceProvider => serviceProvider;
        public bool SaveData  => saveData;
        public string SaveId => saveId;
    }
}
