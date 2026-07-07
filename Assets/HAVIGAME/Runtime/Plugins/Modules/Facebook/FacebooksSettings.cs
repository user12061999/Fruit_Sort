using UnityEngine;

namespace HAVIGAME.Plugins.Facebook {

    [DefineSymbols(FacebookManager.DEFINE_SYMBOL)]
    [SettingMenu(typeof(FacebooksSettings), "Plugins/Facebook", "", "https://developers.facebook.com/docs/unity/gettingstarted", 201, "Icons/icon_facebook.psd")]
    [CreateAssetMenu(fileName = "FacebooksSettings", menuName = "HAVIGAME/Settings/Plugins/Facebook")]
    public class FacebooksSettings : Settings<FacebooksSettings> {
        [SerializeField] private string[] permissions = new string[] { "public_profile", "email" };

        public string[] Permissions => permissions;
    }
}
