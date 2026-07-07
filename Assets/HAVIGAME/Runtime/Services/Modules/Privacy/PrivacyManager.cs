using UnityEngine;

namespace HAVIGAME.Services.Privacy {
    public static class PrivacyManager {
        public const string DEFINE_SYMBOL = "PRIVACY";

        public static readonly InitializeEvent initializeEvent = new InitializeEvent();

        private static IPrivacyService service;

        public static bool IsInitialized => initializeEvent.IsInitialized;

        public static void Initialize() {
#if PRIVACY
            if (initializeEvent.IsRunning) {
                Log.Warning("[Privacy] Cancel initialize! Privacy initialized with result {0}.", initializeEvent.IsInitialized);
                return;
            }

            PrivacySettings settings = PrivacySettings.Instance;

            service = settings.ServiceProvider.GetService();
            service.Initialize();

            Database.Unload(settings);

            Log.Info("[Privacy] Initialize completed.");
            initializeEvent.Invoke(true);
#endif
        }

        public static void RequestAuthorization(System.Action<AuthorizationStatus> onCompleted = null) {
            if (!IsInitialized) {
                Log.Warning("[Privacy] Privacy no initialize!");
                return;
            }

            service.RequestAuthorization(onCompleted);
        }

#if PRIVACY
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {

            public override int Order => SERVICE - 20;
            public override InitializeEvent InitializeEvent => PrivacyManager.initializeEvent;

            public override void Initialize() {
                PrivacyManager.Initialize();
            }
        }
#endif
    }

    [System.Serializable]
    public enum AuthorizationStatus {
        Unknown,
        Authorized,
        Denied,
    }
}