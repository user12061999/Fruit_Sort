using UnityEngine;
using HAVIGAME.Services.Advertisings;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HAVIGAME.Plugins.AppLovin {

    [DefineSymbols(AppLovinManager.DEFINE_SYMBOL)]
    [SettingMenu(typeof(AppLovinSetting), "Plugins/AppLovin", "", "https://dash.applovin.com/documentation/mediation/unity/getting-started/integration", 201, "Icons/icon_applovin.psd")]
    [CreateAssetMenu(fileName = "AppLovinSetting", menuName = "HAVIGAME/Settings/Plugins/AppLovin")]
    public class AppLovinSetting : Settings<AppLovinSetting> {
        [SerializeField] private BoolProperty enable = BoolProperty.Create();
        [SerializeField] private string sdkKey;
        [SerializeReference, Subclass] private AdId[] appOpenAdIds;
        [SerializeReference, Subclass] private AdId[] rewardedAdIds;
        [SerializeReference, Subclass] private AdId[] interstitialAdIds;
        [SerializeReference, Subclass] private AdId[] bannerAdIds;
        [SerializeReference, Subclass] private AdId[] mediumRectangleAdIds;
        [SerializeField] private AdSizeOption bannerAdSize = new AdSizeOption(AdSizeType.AnchoredAdaptive);

        public bool Enable => enable.Get();
        public string SdkKey => sdkKey;
        public AdSizeOption BannerAdSize => bannerAdSize;
        public AdId[] AppOpenAdIds => appOpenAdIds;
        public AdId[] RewardedAdIds => rewardedAdIds;
        public AdId[] InterstitialAdIds => interstitialAdIds;
        public AdId[] BannerAdIds => bannerAdIds;
        public AdId[] MediumRectangleAdIds => mediumRectangleAdIds;

#if UNITY_EDITOR
        [CustomEditor(typeof(AppLovinSetting))]
        private class AppLovinSettingInspector : Editor {

            public override void OnInspectorGUI() {
                base.OnInspectorGUI();

                GUILayout.Space(5);

                if (GUILayout.Button("AppLovin Intgration Manager")) {
                    EditorApplication.ExecuteMenuItem("Assets/AppLovin Integration Manager");
                }
            }
        }
#endif
    }
}
