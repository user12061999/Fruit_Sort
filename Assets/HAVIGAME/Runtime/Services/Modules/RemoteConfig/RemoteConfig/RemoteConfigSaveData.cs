using HAVIGAME.SaveLoad;
using UnityEngine;

namespace HAVIGAME.Services.RemoteConfig {

    [System.Serializable]
    public class RemoteConfigSaveData : SaveData {
        [SerializeField] private string[] keys;
        [SerializeField] private string[] values;

        public int Count => keys.Length;
        public string[] Keys => keys;
        public string[] Values => values;
        public bool IsNullOrEmpty => keys == null || values == null || keys.Length == 0 || values.Length == 0;

        public RemoteConfigSaveData() {
            this.keys = null;
            this.values = null;
        }

        public void UpdateData(RemoteConfigData remoteConfigData) {
            keys = new string[remoteConfigData.Count];
            values = new string[remoteConfigData.Count];

            int i = 0;
            foreach (var item in remoteConfigData.AllValues) {
                keys[i] = item.Key;
                values[i] = item.Value.GetString(string.Empty);
                i++;
            }

            SetChanged();
        }
    }
}
