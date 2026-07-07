using HAVIGAME.Services.Analytics;
using System.Collections.Generic;
using UnityEngine;


#if FACEBOOK
using Facebook.Unity;
#endif

namespace HAVIGAME.Plugins.Facebook {

#if FACEBOOK
    public class FacebookAnalytics : IAnalyticService {
        private bool logAdRevenue;
        private bool logIAPRevenue;

        public InitializeEvent InitializeEvent => FacebookManager.initializeEvent;
        public bool IsInitialized => FacebookManager.IsInitialized;

        public FacebookAnalytics(bool logAdRevenue, bool logIAPRevenue) {
            this.logAdRevenue = logAdRevenue;
            this.logIAPRevenue = logIAPRevenue;
        }

        public void Initialize() {
            Log.Debug("[FacebookAnalytics] Facebook analytics will initialize with Facebook.");
        }

        public void SetProperty(string propertyName, string propertValue) {
            if (!IsInitialized) {
                Log.Warning("[FacebookAnalytics] Facebook no initialize.");
                return;
            }
        }

        public void LogEvent(AnalyticEvent analyticEvent) {
            if (!IsInitialized) {
                Log.Warning("[FacebookAnalytics] Facebook no initialize.");
                return;
            }

            FB.LogAppEvent(analyticEvent.Name, null, analyticEvent.BuildFacebook());
        }

        public void LogAdRevenue(AnalyticAdRevenue analyticRevenue) {
            if (!IsInitialized) {
                Log.Warning("[FacebookAnalytics] Facebook no initialize.");
                return;
            }

            if (!logAdRevenue) {
                Log.Debug("[FacebookAnalytics] Facebook ad revenue event disabled.");
                return;
            }

            Dictionary<string, object> paramaters = new Dictionary<string, object>(6);
            paramaters["ad_platform"] = analyticRevenue.AdPlatform;
            paramaters["ad_source"] = analyticRevenue.AdSource;
            paramaters["ad_unit_name"] = analyticRevenue.AdUnit;
            paramaters["ad_format"] = analyticRevenue.AdFormat;
            paramaters["value"] = analyticRevenue.Value;
            paramaters["currency"] = analyticRevenue.CurrencyCode;

            FB.LogAppEvent("ad_impression", null, paramaters);
        }

        public void LogIAPRevenue(AnalyticIAPRevenue analyticRevenue) {
            if (!IsInitialized) {
                Log.Warning("[FacebookAnalytics] Facebook no initialize.");
                return;
            }

            if (!logIAPRevenue) {
                Log.Debug("[FacebookAnalytics] Facebook iap revenue event disabled.");
                return;
            }

            Dictionary<string, object> paramaters = new Dictionary<string, object>(6);
            paramaters["product_id"] = analyticRevenue.ProductId;
            paramaters["quantity"] = analyticRevenue.Quantity;
            paramaters["value"] = analyticRevenue.Value;
            paramaters["currency"] = analyticRevenue.CurrencyCode;


            FB.LogAppEvent("fb_mobile_purchase", null, paramaters);
        }
    }
#endif

    
    [CategoryMenu("Facebook Analytics")]
    [System.Serializable]
    public class FacebookAnalyticServiceProvider : AnalyticServiceProvider {
        [SerializeField] private bool logAdRevenue;
        [SerializeField] private bool logIAPRevenue;

        public override IAnalyticService GetService() {
#if FACEBOOK
            return new FacebookAnalytics(logAdRevenue, logIAPRevenue);
#else
            return null;
#endif
        }
    }
}
