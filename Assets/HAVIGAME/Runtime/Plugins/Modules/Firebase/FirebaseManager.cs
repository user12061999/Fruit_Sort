using System.Collections;

#if FIREBASE
using Firebase;
using Firebase.Extensions;
using UnityEngine;
#endif

namespace HAVIGAME.Plugins.Firebases {
    public static class FirebaseManager {
        public const string DEFINE_SYMBOL = "FIREBASE";
        public const string ANALYTICS_DEFINE_SYMBOL = "FIREBASE_ANALYTICS";
        public const string CRASHLYTICS_DEFINE_SYMBOL = "FIREBASE_CRASHLYTICS";
        public const string REMOTE_CONFIG_DEFINE_SYMBOL = "FIREBASE_REMOTE_CONFIG";

        public static readonly InitializeEvent initializeEvent = new InitializeEvent();

        public static bool IsInitialized => initializeEvent.IsInitialized;

        public static void Initialize() {
#if FIREBASE
            if (initializeEvent.IsRunning) {
                Log.Warning("[Firebase] Firebase is running with initialize state {0}.", IsInitialized);
                return;
            }

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available) {

                    Log.Info("[Firebase] Initialize completed.");
                    initializeEvent.Invoke(true);
                }
                else {
                    Log.Error("[Firebase] Could not resolve all Firebase dependencies: {0}", dependencyStatus);
                    initializeEvent.Invoke(false);
                }
            });
#endif
        }

#if FIREBASE

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {

            public override int Order => PLUGIN - 100;
            public override InitializeEvent InitializeEvent => FirebaseManager.initializeEvent;

            public override void Initialize() {
                FirebaseManager.Initialize();
            }
        }
#endif
    }
}
