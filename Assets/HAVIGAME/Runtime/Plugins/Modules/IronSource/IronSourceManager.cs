using System.Collections.Generic;
using UnityEngine;

#if IRON_SOURCE
using com.unity3d.mediation;
#endif

namespace HAVIGAME.Plugins.IronSources {
    public static class IronSourceManager {
        public const string DEFINE_SYMBOL = "IRON_SOURCE";

        public static readonly InitializeEvent initializeEvent = new InitializeEvent();

        public static bool IsInitialized => initializeEvent.IsInitialized;

        public static void Initialize() {
#if IRON_SOURCE
            if (initializeEvent.IsRunning) {
                Log.Warning("[IronSource] IronSource is running with initialize state {0}.", IsInitialized);
                return;
            }

            IronSourceSettings setting = IronSourceSettings.Instance;
            
            if (!settings.Enable) {
                Log.Warning("[AppLovin] AppLovin is disabled.");
                return;
            }
            
            GameManager.onApplicationPause += OnApplicationPause;

            LevelPlay.SetPauseGame(true);
            LevelPlay.OnInitSuccess += LevelPlay_OnInitSuccess;
            LevelPlay.OnInitFailed += LevelPlay_OnInitFailed;

            if (Log.DebugEnabled) {
                IronSource.Agent.setMetaData("is_test_suite", "enable");
            }

            List<LevelPlayAdFormat> adFormats = new List<LevelPlayAdFormat>(3);

            adFormats.Add(LevelPlayAdFormat.REWARDED);

            if (setting.InterstitialAdIds != null && setting.InterstitialAdIds.Length > 0) {
                adFormats.Add(LevelPlayAdFormat.INTERSTITIAL);
            }

            if (setting.BannerAdIds != null && setting.BannerAdIds.Length > 0) {
                adFormats.Add(LevelPlayAdFormat.BANNER);
            }

            LevelPlay.Init(setting.AppID, null, adFormats.ToArray());

            setting.Dispose();
#endif
        }


#if IRON_SOURCE
        private static void OnApplicationPause(bool isPaused) {
            IronSource.Agent.onApplicationPause(isPaused);
        }

        private static void LevelPlay_OnInitSuccess(LevelPlayConfiguration configuration) {
            Log.Info($"[IronSource] initialize completed with configuration, ad quality enabled = {configuration.IsAdQualityEnabled}");

            if (Log.DebugEnabled) {
                IronSource.Agent.launchTestSuite();
            }

            initializeEvent.Invoke(true);
        }

        private static void LevelPlay_OnInitFailed(LevelPlayInitError error) {
            Log.Error($"[IronSource] initialize failed with error = {error}");

            initializeEvent.Invoke(false);
        }
#endif

#if IRON_SOURCE
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {
            public override int Order => PLUGIN;
            public override InitializeEvent InitializeEvent => IronSourceManager.initializeEvent;

            public override void Initialize() {
                IronSourceManager.Initialize();
            }
        }
#endif
    }
}
