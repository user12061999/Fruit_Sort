using UnityEngine;

namespace HAVIGAME.Services.Analytics {

    public static class AnalyticManager {

        public const string DEFINE_SYMBOL = "ANALYTICS";

        public static readonly InitializeEvent initializeEvent = new InitializeEvent();

        private static IAnalyticService[] analyticServices;

        public static bool IsInitialized => initializeEvent.IsInitialized;

        public static void Initialize() {
#if ANALYTICS
            if (initializeEvent.IsRunning) {
                Log.Warning("[Analytics] Cancel initialize! Analytics is running initialize state {0}.", IsInitialized);
                return;
            }

            AnalyticsSettings settings = AnalyticsSettings.Instance;
            AnalyticServiceProvider[] providers = settings.GetServiceProviders();

            analyticServices = new IAnalyticService[providers.Length];

            for (int i = 0; i < providers.Length; i++) {
                analyticServices[i] = providers[i].GetService();
                analyticServices[i].Initialize();
            }

            Database.Unload(settings);

            Log.Info("[Analytics] Analytics initialize completed");
            initializeEvent.Invoke(true);
#endif
        }

        public static void SetProperty(string propertyName, string propertyValue) {
            if (!IsInitialized) {
                Log.Warning("[Analytics] Analytics no initialize!");
                return;
            }

            if (Log.DebugEnabled) {
                Log.Debug("[Analytics] Set Property, name = {0}, value = {1}", propertyName, propertyValue);
            }

            foreach (IAnalyticService client in analyticServices) {
                client.SetProperty(propertyName, propertyValue);
            }
        }

        public static void LogEvent(AnalyticEvent analyticalEvent) {
            if (!IsInitialized) {
                Log.Warning("[Analytics] Analytics no initialize!");
                return;
            }

            if (Log.DebugEnabled) {
                Log.Debug("[Analytics] Log Event: {0}", analyticalEvent);
            }

            foreach (IAnalyticService client in analyticServices) {
                client.LogEvent(analyticalEvent);
            }

            ReferencePool.Release(analyticalEvent);
        }

        public static void LogAdRevenue(AnalyticAdRevenue analyticalRevenue) {
            if (!IsInitialized) {
                Log.Warning("[Analytics] Analytics no initialize!");
                return;
            }

            if (Log.DebugEnabled) {
                Log.Debug("[Analytics] Log Revenue: {0}", analyticalRevenue);
            }

            foreach (IAnalyticService client in analyticServices) {
                client.LogAdRevenue(analyticalRevenue);
            }
        }

        public static void LogIAPRevenue(AnalyticIAPRevenue analyticalRevenue) {
            if (!IsInitialized) {
                Log.Warning("[Analytics] Analytics no initialize!");
                return;
            }

            if (Log.DebugEnabled) {
                Log.Debug("[Analytics] Log Revenue: {0}", analyticalRevenue);
            }

            foreach (IAnalyticService client in analyticServices) {
                client.LogIAPRevenue(analyticalRevenue);
            }
        }

#if ANALYTICS
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {

            public override int Order => SERVICE - 50;
            public override InitializeEvent InitializeEvent => AnalyticManager.initializeEvent;

            public override void Initialize() {
                AnalyticManager.Initialize();
            }
        }
#endif
    }
}
