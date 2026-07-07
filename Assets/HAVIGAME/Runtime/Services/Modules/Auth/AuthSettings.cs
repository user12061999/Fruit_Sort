using UnityEngine;

namespace HAVIGAME.Services.Auth {

    [DefineSymbols(AuthManager.DEFINE_SYMBOL)]
    [SettingMenu(typeof(AuthSettings), "Services/Auth", "", null, 101, "Icons/icon_auth.psd")]
    [CreateAssetMenu(fileName = "AuthSettings", menuName = "HAVIGAME/Settings/Services/Auth")]
    public class AuthSettings : Settings<AuthSettings> {

        [SerializeReference, Subclass] private AuthServiceProvider serviceProvider;
        [SerializeField] private bool autoAuth = false;

        public AuthServiceProvider GetServiceProvider() => serviceProvider;
        public bool AutoAuth => autoAuth;
    }
}
