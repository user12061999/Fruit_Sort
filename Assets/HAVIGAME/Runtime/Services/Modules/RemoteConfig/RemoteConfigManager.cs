using HAVIGAME.SaveLoad;
using UnityEngine;

namespace HAVIGAME.Services.RemoteConfig {

    public static class RemoteConfigManager {
        public const string DEFINE_SYMBOL = "REMOTE_CONFIG";

        public static readonly InitializeEvent initializeEvent = new InitializeEvent();

        private static RemoteConfigData remoteConfigData;
        private static IRemoteConfigService remoteConfigService;
        private static DataHolder<RemoteConfigSaveData> dataHolder;

        public static bool IsInitialized => initializeEvent.IsInitialized;

        public static void Initialize() {
#if REMOTE_CONFIG
            if (initializeEvent.IsRunning) {
                Log.Warning("[RemoteConfig] RemoteConfig is running with initialize result {0}", IsInitialized);
                return;
            }

            RemoteConfigSettings settings = RemoteConfigSettings.Instance;

            if (settings.SaveData) {
                dataHolder = SaveLoadManager.Create<RemoteConfigSaveData>(settings.SaveId);

                if (!dataHolder.Data.IsNullOrEmpty) {
                    remoteConfigData = new RemoteConfigData(dataHolder.Data.Keys, dataHolder.Data.Values);
                }
            }

            remoteConfigService = settings.ServiceProvider.GetService();
            remoteConfigService.InitializeEvent.AddListener(OnRemoteConfigServiceInitialized);
            remoteConfigService.Initialize();

            Database.Unload(settings);
#endif
        }

        private static void OnRemoteConfigServiceInitialized(bool isInitialized) {
            if (isInitialized) {
                remoteConfigData = new RemoteConfigData(remoteConfigService.ValueCount);

                foreach (var item in remoteConfigService.AllValues) {
                    remoteConfigData.Add(item.Key, item.Value);
                }

                if (dataHolder != null) {
                    dataHolder.Data.UpdateData(remoteConfigData);
                }

                Log.Info("[RemoteConfig] Initialize completed.");
                initializeEvent.Invoke(true);
            }
            else {
                Log.Error("[RemoteConfig] Initialize failed.");
                initializeEvent.Invoke(false);
            }
        }

        public static string GetStringValue(string key, string defaultValue = "") {
            if (remoteConfigData != null && remoteConfigData.HasKey(key)) {
                return remoteConfigData.GetString(key, defaultValue);
            }
            else {
                return defaultValue;
            }
        }

        public static int GetIntValue(string key, int defaultValue = 0) {
            if (remoteConfigData != null && remoteConfigData.HasKey(key)) {
                return remoteConfigData.GetInt(key, defaultValue);
            }
            else {
                return defaultValue;
            }
        }

        public static bool GetBooleanValue(string key, bool defaultValue = false) {
            if (remoteConfigData != null && remoteConfigData.HasKey(key)) {
                return remoteConfigData.GetBool(key, defaultValue);
            }
            else {
                return defaultValue;
            }
        }

        public static float GetFloatValue(string key, float defaultValue = 0) {
            if (remoteConfigData != null && remoteConfigData.HasKey(key)) {
                return remoteConfigData.GetFloat(key, defaultValue);
            } else {
                return defaultValue;
            }
        }


#if REMOTE_CONFIG
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {

            public override int Order => PLUGIN - 50;
            public override bool WaitForInitialized => true;
            public override InitializeEvent InitializeEvent => RemoteConfigManager.initializeEvent;

            public override void Initialize() {
                RemoteConfigManager.Initialize();
            }
        }
#endif
    }
}
