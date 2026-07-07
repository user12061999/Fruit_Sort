using HAVIGAME.SaveLoad;
using UnityEngine;

namespace HAVIGAME.Plugins.AppsFlyer {
    [System.Serializable]
    public class AppsFlyerSaveData : SaveData {
        [SerializeField] private string countryCode;
        [SerializeField] private string[] keys;
        [SerializeField] private string[] values;

        public int Count => keys.Length;
        public string[] Keys => keys;
        public string[] Values => values;
        public bool IsNullOrEmpty => keys == null || values == null || keys.Length == 0 || values.Length == 0;
        public string CountryCode => countryCode;

        public AppsFlyerSaveData() {
            this.keys = null;
            this.values = null;
            this.countryCode = string.Empty;
        }

        public void UpdateUserCountry(string countryCode) {
            if (this.countryCode != countryCode) {
                this.countryCode = countryCode;
                SetChanged();
            }
        }

        public void UpdateData(AppsFlyerData data) {
            keys = new string[data.Count];
            values = new string[data.Count];

            int i = 0;
            foreach (var item in data.AllValues) {
                keys[i] = item.Key;
                values[i] = item.Value;
                i++;
            }

            SetChanged();
        }
    }
}
