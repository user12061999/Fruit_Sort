using UnityEngine;

namespace HAVIGAME.Plugins.GooglePlayGame {

    [DefineSymbols(GooglePlayGameManager.DEFINE_SYMBOL)]
    [SettingMenu(typeof(GooglePlayGameSettings), "Plugins/Google Play Games", "", "https://developer.android.com/games/pgs/unity/unity-start", 201, "Icons/icon_google_play_games.psd")]
    [CreateAssetMenu(fileName = "GooglePlayGameSettings", menuName = "HAVIGAME/Settings/Plugins/GooglePlayGames")]
    public class GooglePlayGameSettings : Settings<GooglePlayGameSettings> {

    }
}
