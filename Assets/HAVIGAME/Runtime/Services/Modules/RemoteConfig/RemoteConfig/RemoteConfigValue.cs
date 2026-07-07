using System.Globalization;

namespace HAVIGAME.Services.RemoteConfig {
    public class RemoteConfigValue : IReferencePoolable {
        private string data;

        public RemoteConfigValue() {

        }

        public void SetData(string data) {
            this.data = data;
        }

        public string GetString(string defaultValue) {
            if (!string.IsNullOrEmpty(data)) {
                return data;
            }

            return defaultValue;
        }

        public bool GetBool(bool defaultValue) {
            if (!string.IsNullOrEmpty(data) && bool.TryParse(data, out bool result)) {
                return result;
            }

            return defaultValue;
        }

        public int GetInt(int defaultValue) {
            if (!string.IsNullOrEmpty(data) && int.TryParse(data, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)) {
                return result;
            }

            return defaultValue;
        }

        public float GetFloat(float defaultValue) {
            if (!string.IsNullOrEmpty(data) && float.TryParse(data, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float result)) {
                return result;
            }

            return defaultValue;
        }

        public void Clear() {
            data = null;
        }
    }
}
