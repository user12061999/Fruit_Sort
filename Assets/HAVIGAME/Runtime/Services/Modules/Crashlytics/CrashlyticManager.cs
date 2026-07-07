using UnityEngine;

namespace HAVIGAME.Services.Crashlytics {
    public static class CrashlyticManager {
        public const string DEFINE_SYMBOL = "CRASHLYTICS";

        public static readonly InitializeEvent initializeEvent = new InitializeEvent();

        private static ICrashlyticService[] crashlyticServices;

        public static bool IsInitialized => initializeEvent.IsInitialized;

        public static void Initialize() {
#if CRASHLYTICS
            if (initializeEvent.IsRunning) {
                Log.Warning("[Crashlytics] Cancel initialize! Crashlytics is running initialize state {0}.", IsInitialized);
                return;
            }

            CrashlyticsSettings settings = CrashlyticsSettings.Instance;
            CrashlyticServiceProvider[] providers = settings.GetServiceProviders();

            if (providers.Length <= 0) {
                Log.Error("[Crashlytics] Initialize failed! Crashlytics services is empty.");
                initializeEvent.Invoke(false);
                return;
            }

            crashlyticServices = new ICrashlyticService[providers.Length];
            for (int i = 0; i < providers.Length; i++) {
                crashlyticServices[i] = providers[i].GetService();
                crashlyticServices[i].Initialize();
            }

            Database.Unload(settings);

            Log.Info("[Crashlytics] Crashlytics initialize completed.");
            initializeEvent.Invoke(true);
#endif
        }

        public static void LogMessage(string message) {
            if (!IsInitialized) {
                Log.Warning("[Crashlytics] Crashlytics no initialize!");
                return;
            }

            if (Log.DebugEnabled) {
                Log.Debug("[Crashlytics] Log message: {0}", message);
            }

            foreach (ICrashlyticService service  in crashlyticServices) {
                service.LogMessage(message);
            }
        }

        public static void LogException(System.Exception exception) {
            if (!IsInitialized) {
                Log.Warning("[Crashlytics] Crashlytics no initialize!");
                return;
            }

            if (Log.DebugEnabled) {
                Log.Debug("[Crashlytics] Log exception: {0}", exception);
            }

            foreach (ICrashlyticService service in crashlyticServices) {
                service.LogException(exception);
            }
        }


#if CRASHLYTICS
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {

            public override int Order => SERVICE;
            public override InitializeEvent InitializeEvent => CrashlyticManager.initializeEvent;

            public override void Initialize() {
                CrashlyticManager.Initialize();
            }
        }
#endif
    }
}
