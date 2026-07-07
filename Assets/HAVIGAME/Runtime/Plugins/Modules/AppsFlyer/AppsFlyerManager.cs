using HAVIGAME.SaveLoad;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Collections;

#if APPSFLYER
using AppsFlyerSDK;
#endif

namespace HAVIGAME.Plugins.AppsFlyer {
#if APPSFLYER
    public class AppsFlyerManager : Singleton<AppsFlyerManager>, IAppsFlyerConversionData, IAppsFlyerValidateAndLog {
#else
    public class AppsFlyerManager : Singleton<AppsFlyerManager> {
#endif
        public const string DEFINE_SYMBOL = "APPSFLYER";

        private static int requestIndex = 0;

#if APPSFLYER
        public static readonly CountryAPI[] countryAPIs = new CountryAPI[] {
            new CountryAPI(@"https://freeipapi.com/api/json", "countryCode"),
            new CountryAPI(@"https://api.ipbase.com/v1/json", "country_code"),
            new CountryAPI(@"https://api.country.is", "country"),
        };
#endif

        public static readonly InitializeEvent initializeEvent = new InitializeEvent();

        private static AppsFlyerData appsflyerData;
        private static DataHolder<AppsFlyerSaveData> dataHolder;
        private static Action<string> onValidatePurchaseCompleted;
        private static Action<string> onValidatePurchaseFailed;

        public static bool IsInitialized => initializeEvent.IsInitialized;

        public static string CountryCode => dataHolder.Data.CountryCode;
        public static bool IsOrganic => GetConversionValue("af_status", "Organic").Equals("Organic");
        public static string MediaSource => GetConversionValue("media_source");
        public static string Campaign => GetConversionValue("campaign");
        public static string AdSet => GetConversionValue("af_adset");

        public static void Initialize() {
#if APPSFLYER
            if (initializeEvent.IsRunning) {
                Log.Warning("[AppsFlyer] AppsFlyer is running with initialize state {0}.", IsInitialized);
                return;
            }

            AppsFlyerManager.Instance.Create();

            AppsFlyerSettings settings = AppsFlyerSettings.Instance;

            if (settings.SaveConversionData) {
                dataHolder = SaveLoadManager.Create<AppsFlyerSaveData>(settings.SaveId);

                if (!dataHolder.Data.IsNullOrEmpty) {
                    appsflyerData = new AppsFlyerData(dataHolder.Data.Keys, dataHolder.Data.Values);
                }
            }

            AppsFlyerSDK.AppsFlyer.initSDK(settings.DevKey, settings.AppID, AppsFlyerManager.Instance);

#if UNITY_IOS
            AppsFlyerSDK.AppsFlyer.waitForATTUserAuthorizationWithTimeoutInterval(60);
#endif

            AppsFlyerSDK.AppsFlyer.setUseReceiptValidationSandbox(settings.SanboxMode);
            AppsFlyerSDK.AppsFlyer.setUseUninstallSandbox(settings.SanboxMode);
            AppsFlyerSDK.AppsFlyer.startSDK();

            if (string.IsNullOrEmpty(CountryCode)) {
                requestIndex = 0;
                RequestUserCountry();
            }

            Database.Unload(settings);

            Log.Info("[AppsFlyer] Initialize completed.");
            initializeEvent.Invoke(true);
#endif
        }

        public static string GetConversionValue(string key, string defaultValue = "not_match") {
            if (appsflyerData != null && appsflyerData.HasKey(key)) {
                return appsflyerData.GetString(key, defaultValue);
            } else {
                return defaultValue;
            }
        }

#if APPSFLYER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public void onAppOpenAttribution(string attributionData) {
            Log.Info(Utility.Text.Format("[AppsFlyerManager] App open attribution successed with data = {0}", attributionData));
        }

        public void onAppOpenAttributionFailure(string error) {
            Log.Error(Utility.Text.Format("[AppsFlyerManager] App open attribution failed with error = {0}", error));
        }

        public void onConversionDataFail(string error) {
            Log.Error(Utility.Text.Format("[AppsFlyerManager] Conversion data failed with error = {0}", error));
        }

        public void onConversionDataSuccess(string conversionData) {
            Log.Debug($"[AppsFlyerManager] Conversion data successed.");

            Dictionary<string, object> conversionDataDictionary = AppsFlyerSDK.AppsFlyer.CallbackStringToDictionary(conversionData);

            if (appsflyerData == null) {
                appsflyerData = new AppsFlyerData(conversionDataDictionary.Count);
            } else {
                appsflyerData.Clear();
            }

            foreach (var item in conversionDataDictionary) {
                appsflyerData.Add(item.Key, item.Value != null ? item.Value.ToString() : "null");

                if (Log.DebugEnabled) {
                    Log.Debug($"key = {item.Key}, value = {(item.Value != null ? item.Value.ToString() : "null")}");
                }
            }

            if (dataHolder != null) {
                dataHolder.Data.UpdateData(appsflyerData);
            }
        }

        public static void ValidateAndSendInAppPurchase(AFPurchaseDetailsAndroid details, Dictionary<string, string> additionalParameters, Action<string> onCompleted, Action<string> onFailed) {
            onValidatePurchaseCompleted = onCompleted;
            onValidatePurchaseFailed = onFailed;
            AppsFlyerSDK.AppsFlyer.validateAndSendInAppPurchase(details, additionalParameters, Instance);
        }

        public static void ValidateAndSendInAppPurchase(AFSDKPurchaseDetailsIOS details, Dictionary<string, string> additionalParameters, Action<string> onCompleted, Action<string> onFailed) {
            onValidatePurchaseCompleted = onCompleted;
            onValidatePurchaseFailed = onFailed;
            AppsFlyerSDK.AppsFlyer.validateAndSendInAppPurchase(details, additionalParameters, Instance);
        }

        public void onValidateAndLogComplete(string result) {
            onValidatePurchaseCompleted?.Invoke(result);
        }

        public void onValidateAndLogFailure(string error) {
            onValidatePurchaseFailed?.Invoke(error);
        }

        private static void RequestUserCountry() {
            if (requestIndex >= 0 && requestIndex < countryAPIs.Length) {
                CountryAPI countryAPI = countryAPIs[requestIndex];
                string url = countryAPI.url;
                string key = countryAPI.key;

                Request(url,
                    content => {
                        if (Log.DebugEnabled) Log.Debug(content);

                        Dictionary<string, object> dataDictionary = AppsFlyerSDK.AppsFlyer.CallbackStringToDictionary(content);

                        string countryCode = null;

                        if (dataDictionary.TryGetValue(key, out object countryCodeObject)) {
                            if (countryCodeObject != null) countryCode = countryCodeObject.ToString();
                        }

                        if (!string.IsNullOrEmpty(countryCode)) {
                            dataHolder.Data.UpdateUserCountry(countryCode);
                        } else {
                            requestIndex++;
                            RequestUserCountry();
                        }

                    }, error => {
                        Log.Error(error);

                        requestIndex++;
                        RequestUserCountry();
                    });
            }
        }

        private static void Request(string url, Action<string> onCompleted, Action<string> onFailed) {
            Executor.Instance.Run(IERequest(url, onCompleted, onFailed));
        }

        private static IEnumerator IERequest(string url, Action<string> onCompleted, Action<string> onFailed) {

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url)) {
                Log.Debug($"Start request, url = {url}.");

                yield return webRequest.SendWebRequest();

                switch (webRequest.result) {
                    case UnityWebRequest.Result.InProgress:
                        onFailed?.Invoke($"In progress error: {webRequest.error}");
                        break;
                    case UnityWebRequest.Result.Success:
                        onCompleted?.Invoke(webRequest.downloadHandler.text);
                        break;
                    case UnityWebRequest.Result.ConnectionError:
                        onFailed?.Invoke($"Connection error: {webRequest.error}");
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        onFailed?.Invoke($"Protocol error: {webRequest.error}");
                        break;
                    case UnityWebRequest.Result.DataProcessingError:
                        onFailed?.Invoke($"Data processing error: {webRequest.error}");
                        break;
                    default:
                        onFailed?.Invoke($"Unknown error.");
                        break;
                }

                Log.Debug($"Finish request, url = {url}.");
            }
        }

        public struct CountryAPI {
            public string url;
            public string key;

            public CountryAPI(string url, string key) {
                this.url = url;
                this.key = key;
            }
        }

        public class Initializer : ModuleInitializer {

            public override int Order => PLUGIN;
            public override InitializeEvent InitializeEvent => AppsFlyerManager.initializeEvent;

            public override void Initialize() {
                AppsFlyerManager.Initialize();
            }
        }
#endif
    }
}
