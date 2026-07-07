using System.Collections.Generic;
using UnityEngine;
using HAVIGAME.Services.Analytics;

namespace HAVIGAME.Plugins.AppsFlyer {

#if APPSFLYER
    public class AppsFlyerAnalytics : IAnalyticService {
        private bool logAdRevenue;
        private bool logIAPRevenue;

        public InitializeEvent InitializeEvent => AppsFlyerManager.initializeEvent;

        public bool IsInitialized => AppsFlyerManager.IsInitialized;

        public AppsFlyerAnalytics(bool logAdRevenue, bool logIAPRevenue) {
            this.logAdRevenue = logAdRevenue;
            this.logIAPRevenue = logIAPRevenue;
        }

        public void Initialize() {
            Log.Debug("[AppsFlyerAnalytics] AppsFlyer analytics will initialize with AppsFlyer.");

            AppsFlyerManager.initializeEvent.AddListener(OnAppsFlyerInitialize);
        }

        private void OnAppsFlyerInitialize(bool initialized) {
            if (initialized) {
                Log.Info("[AppsFlyerAnalytics] Initialize completed.");
            } else {
                Log.Error("[AppsFlyerAnalytics] Initialize failed.");
            }
        }

        public void SetProperty(string propertyName, string propertValue) {
            if (!IsInitialized) {
                Log.Warning("[AppsFlyerAnalytics] Appsflyer no initialize.");
                return;
            }
        }

        public void LogEvent(AnalyticEvent analyticEvent) {
            if (!IsInitialized) {
                Log.Warning("[AppsFlyerAnalytics] Appsflyer no initialize.");
                return;
            }

            AppsFlyerSDK.AppsFlyer.sendEvent(analyticEvent.Name, analyticEvent.BuildAppsflyer());
        }

        public void LogAdRevenue(AnalyticAdRevenue analyticRevenue) {
            if (!IsInitialized) {
                Log.Warning("[AppsFlyerAnalytics] Appsflyer no initialize.");
                return;
            }

            if (!logAdRevenue) {
                Log.Debug("[AppsFlyerAnalytics] Appsflyer ad revenue event disabled.");
                return;
            }

            AppsFlyerSDK.AFAdRevenueData adRevenueData = new AppsFlyerSDK.AFAdRevenueData(
                analyticRevenue.AdSource,
                GetMediationNetworkType(analyticRevenue.AdPlatform),
                analyticRevenue.CurrencyCode,
                analyticRevenue.Value);

            AppsFlyerSDK.AppsFlyer.logAdRevenue(adRevenueData, null);
        }

        public void LogIAPRevenue(AnalyticIAPRevenue analyticRevenue) {
            if (!IsInitialized) {
                Log.Warning("[AppsFlyerAnalytics] Appsflyer no initialize.");
                return;
            }

            if (!logIAPRevenue) {
                Log.Debug("[AppsFlyerAnalytics] Appsflyer iap revenue event disabled.");
                return;
            }

            Dictionary<string, string> paramaters = new Dictionary<string, string>() {
                {"af_content_id", analyticRevenue.ProductId },
                {"af_quantity", analyticRevenue.Quantity.ToString() },
                {"af_revenue", analyticRevenue.Value.ToString() },
                {"af_currency", analyticRevenue.CurrencyCode },
            };

            AppsFlyerSDK.AppsFlyer.sendEvent("af_purchase", paramaters);
        }

        private AppsFlyerSDK.MediationNetwork GetMediationNetworkType(string mediation) {
            switch (mediation) {
                case "AdMob":
                    return AppsFlyerSDK.MediationNetwork.GoogleAdMob;
                case "AppLovin":
                    return AppsFlyerSDK.MediationNetwork.ApplovinMax;
                case "IronSource":
                    return AppsFlyerSDK.MediationNetwork.IronSource;
                default:
                    return AppsFlyerSDK.MediationNetwork.ApplovinMax;
            }
        }
    }
#endif


    [CategoryMenu("AppsFlyer Analytics")]
    [System.Serializable]
    public class AppsflyerAnalyticServiceProvider : AnalyticServiceProvider {
        [SerializeField] private bool logAdRevenue;
        [SerializeField] private bool logIAPRevenue;

        public override IAnalyticService GetService() {
#if APPSFLYER
            return new AppsFlyerAnalytics(logAdRevenue, logIAPRevenue);
#else
            return null;
#endif
        }
    }
}