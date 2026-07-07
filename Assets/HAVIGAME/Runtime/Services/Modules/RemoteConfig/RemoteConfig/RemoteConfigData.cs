using System.Collections.Generic;

namespace HAVIGAME.Services.RemoteConfig {
    public class RemoteConfigData {
        private Dictionary<string, RemoteConfigValue> configValues;

        public int Count => configValues.Count;

        public IEnumerable<KeyValuePair<string, RemoteConfigValue>> AllValues => configValues;

        public RemoteConfigData(int capacity) {
            configValues = new Dictionary<string, RemoteConfigValue>(capacity);
        }

        public RemoteConfigData(string[] keys, string[] values) {
            configValues = new Dictionary<string, RemoteConfigValue>(keys.Length);

            for (int i = 0; i < keys.Length; i++) {
                Add(keys[i], values[i]);
            }
        }

        public void Add(string key, string value) {
            RemoteConfigValue configValue = ReferencePool.Acquire<RemoteConfigValue>();
            configValue.SetData(value);

            configValues.Add(key, configValue);
        }

        public void Remove(string key) {
            if (configValues.Remove(key, out RemoteConfigValue value)) {
                ReferencePool.Release(value);
            }
        }

        public void Clear() {
            foreach (RemoteConfigValue value in configValues.Values) {
                ReferencePool.Release(value);
            }
            configValues.Clear();
        }

        public bool HasKey(string key) {
            return configValues.ContainsKey(key);
        }

        public string GetString(string key, string defaultValue) {
            return configValues[key].GetString(defaultValue);
        }

        public bool GetBool(string key, bool defaultValue) {
            return configValues[key].GetBool(defaultValue);
        }

        public int GetInt(string key, int defaultValue) {
            return configValues[key].GetInt(defaultValue);
        }

        public float GetFloat(string key, float defaultValue) {
            return configValues[key].GetFloat(defaultValue);
        }
    }
}
