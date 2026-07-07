using System.Collections.Generic;

namespace HAVIGAME.Plugins.AppsFlyer {
    public class AppsFlyerData {
        private Dictionary<string, string> conversionDatas;

        public int Count => conversionDatas.Count;

        public IEnumerable<KeyValuePair<string, string>> AllValues => conversionDatas;

        public AppsFlyerData(int capacity) {
            conversionDatas = new Dictionary<string, string>(capacity);
        }

        public AppsFlyerData(string[] keys, string[] values) {
            conversionDatas = new Dictionary<string, string>(keys.Length);

            for (int i = 0; i < keys.Length; i++) {
                Add(keys[i], values[i]);
            }
        }

        public void Add(string key, string value) {
            conversionDatas.Add(key, value);
        }

        public void Remove(string key) {
            conversionDatas.Remove(key);
        }

        public void Clear() {
            conversionDatas.Clear();
        }

        public bool HasKey(string key) {
            return conversionDatas.ContainsKey(key);
        }

        public string GetString(string key, string defaultValue) {
            if (conversionDatas.ContainsKey(key)) {
                return conversionDatas[key];
            } else {
                return defaultValue;
            }
        }
    }
}