using UnityEngine;

namespace HAVIGAME.Plugins.AppLovin {
    public static class AppLovinManager {
        public const string DEFINE_SYMBOL = "APPLOVIN";

        public static readonly InitializeEvent initializeEvent = new InitializeEvent();

        public static bool IsInitialized => initializeEvent.IsInitialized;

        public static void Initialize() {
#if APPLOVIN
            if (initializeEvent.IsRunning) {
                Log.Warning("[AppLovin] AppLovin is running with initialize state {0}.", IsInitialized);
                return;
            }

            AppLovinSetting setting = AppLovinSetting.Instance;

            if (!setting.Enable) {
                Log.Warning("[AppLovin] AppLovin is disabled.");
                return;
            }
            
            MaxSdkCallbacks.OnSdkInitializedEvent += OnSdkInitializedEvent;
            MaxSdk.SetSdkKey(setting.SdkKey);
            MaxSdk.SetVerboseLogging(Log.DebugEnabled);
            MaxSdk.InitializeSdk();
#endif
        }

#if APPLOVIN
        private static void OnSdkInitializedEvent(MaxSdkBase.SdkConfiguration config) {
            if (config.IsSuccessfullyInitialized) {
                Log.Info("[AppLovin] AppLovin initialize completed, country = {0}, test mode = {1}", config.CountryCode, config.IsTestModeEnabled);

                initializeEvent.Invoke(true);
            }
            else {
                Log.Error("[AppLovin] AppLovin initialize failed, country = {0}, test mode = {1}", config.CountryCode, config.IsTestModeEnabled);

                initializeEvent.Invoke(false);
            }
        }
#endif

#if APPLOVIN
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {
            public override int Order => PLUGIN;
            public override InitializeEvent InitializeEvent => AppLovinManager.initializeEvent;

            public override void Initialize() {
                AppLovinManager.Initialize();
            }
        }
#endif
    }
}
