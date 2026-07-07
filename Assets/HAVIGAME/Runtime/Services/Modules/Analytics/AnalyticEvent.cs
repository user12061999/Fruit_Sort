using System.Collections;
using System.Collections.Generic;

namespace HAVIGAME.Services.Analytics {
    public class AnalyticEvent : IReferencePoolable {
        private string name;
        private Dictionary<string, AnalyticParamter> paramaters;

        public string Name => name;

        public Dictionary<string, AnalyticParamter> Paramaters => paramaters;

        public AnalyticEvent() {
            this.name = string.Empty;
            this.paramaters = new Dictionary<string, AnalyticParamter>(4);
        }

        public void Clear() {
            this.name = string.Empty;
            this.paramaters.Clear();
        }

        public static AnalyticEvent Create(string name) {
            AnalyticEvent analyticEvent = ReferencePool.Acquire<AnalyticEvent>();
            analyticEvent.name = name;
            return analyticEvent;
        }

        public AnalyticEvent Add(string paramaterName, AnalyticParamter paramterValue) {
            paramaters[paramaterName] = paramterValue;
            return this;
        }

#if FIREBASE_ANALYTICS
        public Firebase.Analytics.Parameter[] BuildFirebase() {
            Firebase.Analytics.Parameter[] paramaters = new Firebase.Analytics.Parameter[this.paramaters.Count];

            int index = 0;
            foreach (var item in this.paramaters) {
                switch (item.Value.Type) {
                    case AnalyticParamaterType.Long:
                        paramaters[index] = new Firebase.Analytics.Parameter(item.Key, item.Value.GetLong(0));
                        break;
                    case AnalyticParamaterType.Double:
                        paramaters[index] = new Firebase.Analytics.Parameter(item.Key, item.Value.GetDouble(0));
                        break;
                    default:
                        string content = item.Value.GetString("NULL");
                        if (content.Length > 90) content = content.Substring(0, 90);
                        paramaters[index] = new Firebase.Analytics.Parameter(item.Key, content);
                        break;
                }
                index++;
            }

            return paramaters;
        }
#endif

#if APPSFLYER
        public Dictionary<string, string> BuildAppsflyer() {
            Dictionary<string, string> paramaters = new Dictionary<string, string>(this.paramaters.Count);

            foreach (var item in this.paramaters) {
                paramaters[item.Key] = item.Value.GetString("NULL");
            }

            return paramaters;
        }
#endif

#if FACEBOOK
        public Dictionary<string, object> BuildFacebook() {
            Dictionary<string, object> param = new Dictionary<string, object>(Paramaters.Count);

            foreach (var paramater in Paramaters) {
                param[paramater.Key] = paramater.Value;
            }

            return param;
        }
#endif

        public override string ToString() {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(Name);
            foreach (var item in paramaters) {
                sb.Append($"\n{item.Key}: {item.Value}");
            }
            return sb.ToString();
        }
    }

    public struct AnalyticParamter {
        private string data;
        private AnalyticParamaterType type;

        public AnalyticParamaterType Type => type;

        public AnalyticParamter(string data, AnalyticParamaterType type) {
            this.data = data;
            this.type = type;
        }

        public string GetString(string defaultValue) {
            if (!string.IsNullOrEmpty(data)) {
                return data;
            }

            return defaultValue;
        }

        public long GetLong(long defaultValue) {
            if (!string.IsNullOrEmpty(data) && long.TryParse(data, out long result)) {
                return result;
            }

            return defaultValue;
        }

        public double GetDouble(double defaultValue) {
            if (!string.IsNullOrEmpty(data) && double.TryParse(data, out double result)) {
                return result;
            }

            return defaultValue;
        }

        public override string ToString() {
            switch (type) {
                case AnalyticParamaterType.Long: return GetLong(0).ToString();
                case AnalyticParamaterType.Double: return GetDouble(0).ToString();
                default: return GetString("NULL");
            }
        }
    }

    public enum AnalyticParamaterType {
        String = 0,
        Long = 1,
        Double = 2,
    }
}

