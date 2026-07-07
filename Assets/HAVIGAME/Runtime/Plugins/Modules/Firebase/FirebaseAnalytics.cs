using HAVIGAME.Services.Analytics;
using UnityEngine;

namespace HAVIGAME.Plugins.Firebases {
#if FIREBASE && FIREBASE_ANALYTICS
    public class FirebaseAnalytics : IAnalyticService {
        private bool logAdRevenue;
        private bool logIAPRevenue;

        public static readonly InitializeEvent initializeEvent = new InitializeEvent();

        public InitializeEvent InitializeEvent => initializeEvent;

        public bool IsInitialized => initializeEvent.IsInitialized;

        public FirebaseAnalytics(bool logAdRevenue, bool logIAPRevenue) {
            this.logAdRevenue = logAdRevenue;
            this.logIAPRevenue = logIAPRevenue;
        }

        public void Initialize() {
            if (initializeEvent.IsRunning) {
                Log.Warning("[FirebaseAnalytics] Firebase analytics  is running with initialize state {0}.", IsInitialized);
                return;
            }

            Log.Info("[FirebaseAnalytics] Initialize completed.");
            initializeEvent.Invoke(true);
        }

        public void SetProperty(string propertyName, string propertValue) {
            if (!IsInitialized) {
                Log.Warning("[FirebaseAnalytics] Firebase no initialize.");
                return;
            }

            Firebase.Analytics.FirebaseAnalytics.SetUserProperty(propertyName, propertValue);
        }

        public void LogEvent(AnalyticEvent analyticEvent) {
            if (!IsInitialized) {
                Log.Warning("[FirebaseAnalytics] Firebase no initialize.");
                return;
            }

            Firebase.Analytics.FirebaseAnalytics.LogEvent(analyticEvent.Name, analyticEvent.BuildFirebase());
        }

        public void LogAdRevenue(AnalyticAdRevenue analyticRevenue) {
            if (!IsInitialized) {
                Log.Warning("[FirebaseAnalytics] Firebase no initialize.");
                return;
            }

            if (!logAdRevenue) {
                Log.Debug("[FirebaseAnalytics] Firebase ad revenue event disabled.");
                return;
            }

            Firebase.Analytics.Parameter[] paramaters = new Firebase.Analytics.Parameter[6];
            paramaters[0] = new Firebase.Analytics.Parameter("ad_platform", analyticRevenue.AdPlatform);
            paramaters[1] = new Firebase.Analytics.Parameter("ad_source", analyticRevenue.AdSource);
            paramaters[2] = new Firebase.Analytics.Parameter("ad_unit_name", analyticRevenue.AdUnit);
            paramaters[3] = new Firebase.Analytics.Parameter("ad_format", analyticRevenue.AdFormat);
            paramaters[4] = new Firebase.Analytics.Parameter("value", analyticRevenue.Value);
            paramaters[5] = new Firebase.Analytics.Parameter("currency", analyticRevenue.CurrencyCode);

            Firebase.Analytics.FirebaseAnalytics.LogEvent("ad_impression", paramaters);
        }

        public void LogIAPRevenue(AnalyticIAPRevenue analyticRevenue) {
            if (!IsInitialized) {
                Log.Warning("[FirebaseAnalytics] Firebase no initialize.");
                return;
            }

            if (!logIAPRevenue) {
                Log.Debug("[FirebaseAnalytics] Firebase iap revenue event disabled.");
                return;
            }

            Firebase.Analytics.Parameter[] paramaters = new Firebase.Analytics.Parameter[4];
            paramaters[0] = new Firebase.Analytics.Parameter("product_id", analyticRevenue.ProductId);
            paramaters[1] = new Firebase.Analytics.Parameter("quantity", analyticRevenue.Quantity);
            paramaters[2] = new Firebase.Analytics.Parameter("value", analyticRevenue.Value.ToString());
            paramaters[3] = new Firebase.Analytics.Parameter("currentcy", analyticRevenue.CurrencyCode);

            Firebase.Analytics.FirebaseAnalytics.LogEvent("in_app_purchase", paramaters);
        }
    }
#endif

    [CategoryMenu("Firebase Analytics")]
    [System.Serializable]
    public class FirebaseAnalyticServiceProvider : AnalyticServiceProvider {
        [SerializeField] private bool logAdRevenue;
        [SerializeField] private bool logIAPRevenue;

        public override IAnalyticService GetService() {
#if FIREBASE && FIREBASE_ANALYTICS
            return new FirebaseAnalytics(logAdRevenue, logIAPRevenue);
#else
            return null;
#endif
        }
    }
}