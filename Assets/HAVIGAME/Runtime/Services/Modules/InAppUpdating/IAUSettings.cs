using UnityEngine;

namespace HAVIGAME.Services.IAU {

    [DefineSymbols(IAUManager.DEFINE_SYMBOL)]
    [SettingMenu(typeof(IAUSettings), "Services/In-App Updating", "", "https://developer.android.com/guide/playcore/in-app-updates/unity", 101, "Icons/icon_iau.psd")]
    [CreateAssetMenu(fileName = "IAUSettings", menuName = "HAVIGAME/Settings/Services/In-App Updating")]
    public class IAUSettings : Settings<IAUSettings> {
        [SerializeField] private UpdateMode updateMode = UpdateMode.Immediate;
        [SerializeField] private bool allowAssetPackDeletion = false;

        public UpdateMode UpdateMode => updateMode;
        public bool AllowAssetPackDeletion => allowAssetPackDeletion;
    }


    [System.Serializable]
    public enum UpdateMode {
        Immediate,
        Flexible,
    }
}
