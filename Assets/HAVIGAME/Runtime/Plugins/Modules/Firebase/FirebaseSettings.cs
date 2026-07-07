using UnityEngine;

namespace HAVIGAME.Plugins.Firebases {

    [DefineSymbols(FirebaseManager.DEFINE_SYMBOL)]
    [SettingMenu(typeof(FirebaseSettings), "Plugins/Firebase", "", "https://firebase.google.com/docs/unity/setup", 201, "Icons/icon_firebase.psd")]
    [CreateAssetMenu(fileName = "FirebaseSettings", menuName = "HAVIGAME/Settings/Plugins/Firebase")]
    public class FirebaseSettings : Settings<FirebaseSettings> {
        [SerializeField, DefineCondition(FirebaseManager.ANALYTICS_DEFINE_SYMBOL)] private bool analyticEnabled;
        [SerializeField, DefineCondition(FirebaseManager.CRASHLYTICS_DEFINE_SYMBOL)] private bool crashlyticEnabled;
        [SerializeField, DefineCondition(FirebaseManager.REMOTE_CONFIG_DEFINE_SYMBOL)] private bool remoteConfigEnabled;
        [SerializeField] private bool fetchAndActiveOnInitialize = true;

        public bool FetchAndActiveOnInitialize => fetchAndActiveOnInitialize;
    }
}
