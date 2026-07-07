#if FACEBOOK
using Facebook.Unity;
using UnityEngine;
#endif

namespace HAVIGAME.Plugins.Facebook {
    public static partial class FacebookManager {

        public const string DEFINE_SYMBOL = "FACEBOOK";

        public static InitializeEvent initializeEvent = new InitializeEvent();

        public static bool IsInitialized => initializeEvent.IsInitialized;

        public static void Initialize() {
#if FACEBOOK
            if (initializeEvent.IsRunning) {
                Log.Warning("[Facebook] Facebook is running with initialize state {0}.", IsInitialized);
                return;
            }

            FB.Init(OnInitCompleted, OnHideUnity);
#endif
        }

#if FACEBOOK
        private static void OnInitCompleted() {
            if (FB.IsInitialized) {
                FB.ActivateApp();

                Log.Info("[Facebook] Initialize completed.");
                initializeEvent.Invoke(true);
            }
            else {
                Log.Error("[Facebook] Initialize failed.");
                initializeEvent.Invoke(false);
            }
        }

        private static void OnHideUnity(bool isGameShown) {

        }
#endif


#if FACEBOOK
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {

            public override int Order => PLUGIN;
            public override InitializeEvent InitializeEvent => FacebookManager.initializeEvent;

            public override void Initialize() {
                FacebookManager.Initialize();
            }
        }
#endif
    }
}