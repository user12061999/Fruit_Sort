using System.Collections;
using System.Collections.Generic;
using HAVIGAME;
using HAVIGAME.Services.Analytics;

public static class GameAnalytics {
    public class GameEvent : IReferencePoolable {
        private string name;
        private Dictionary<string, GameParamter> eventParamaters;

        public string Name => name;
        public Dictionary<string, GameParamter> Paramaters => eventParamaters;

        public GameEvent() {
            name = string.Empty;
            eventParamaters = new Dictionary<string, GameParamter>(4);
        }

        public void Clear() {
            name = string.Empty;
            eventParamaters.Clear();
        }

        public static GameEvent Create(string name) {
            GameEvent gameEvent = ReferencePool.Acquire<GameEvent>();
            gameEvent.name = name;
            return gameEvent;
        }

        public GameEvent Add(string parameterName, object parameterValue) {
            eventParamaters[parameterName] = new GameParamter(parameterValue.ToString(), ParamaterType.String);
            return this;
        }

        public GameEvent Add(string parameterName, string parameterValue) {
            eventParamaters[parameterName] = new GameParamter(parameterValue, ParamaterType.String);
            return this;
        }

        public GameEvent Add(string parameterName, double parameterValue) {
            eventParamaters[parameterName] = new GameParamter(parameterValue.ToString(), ParamaterType.Double);
            return this;
        }

        public GameEvent Add(string parameterName, long parameterValue) {
            eventParamaters[parameterName] = new GameParamter(parameterValue.ToString(), ParamaterType.Long);
            return this;
        }

        public override string ToString() {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(Name);
            foreach (var item in eventParamaters) {
                sb.Append($"\n{item.Key}: {item.Value}");
            }
            return sb.ToString();
        }

    }

    public struct GameAdRevenue {
        private string adPlatform;
        private string adSource;
        private string adUnit;
        private string adFormat;
        private double value;
        private string currencyCode;

        public string AdPlatform => adPlatform;
        public string AdSource => adSource;
        public string AdUnit => adUnit;
        public string AdFormat => adFormat;
        public double Value => value;
        public string CurrencyCode => currencyCode;

        public GameAdRevenue(string adPlatform, string adSource, string adUnit, string adFormat, double value, string currencyCode) {
            this.adPlatform = adPlatform;
            this.adSource = adSource;
            this.adUnit = adUnit;
            this.adFormat = adFormat;
            this.value = value;
            this.currencyCode = currencyCode;
        }
    }

    public struct GameIAPRevenue {
        private string productId;
        private int quantity;
        private decimal value;
        private string currencyCode;

        public string ProductId => productId;
        public int Quantity => quantity;
        public decimal Value => value;
        public string CurrencyCode => currencyCode;


        public GameIAPRevenue(string productId, int quantity, decimal value, string currencyCode) {
            this.productId = productId;
            this.quantity = quantity;
            this.value = value;
            this.currencyCode = currencyCode;
        }
    }

    public struct GameParamter {
        private string data;
        private ParamaterType type;

        public ParamaterType Type => type;

        public GameParamter(string data, ParamaterType type) {
            this.data = data;
            this.type = type;
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

        public AnalyticParamter GetAnalyticParamter() {
            switch (type) {
                case ParamaterType.Long:
                    return new AnalyticParamter(data, AnalyticParamaterType.Long);
                case ParamaterType.Double:
                    return new AnalyticParamter(data, AnalyticParamaterType.Double);
                default:
                    return new AnalyticParamter(data, AnalyticParamaterType.String);
            }
        }

        public override string ToString() {
            switch (type) {
                case ParamaterType.Long: return GetLong(0).ToString();
                case ParamaterType.Double: return GetDouble(0).ToString();
                default: return GetString("NULL");
            }
        }
    }

    public enum ParamaterType {
        String = 0,
        Long = 1,
        Double = 2,
    }

    public static void SetProperty(string propertyName, string propertyValue) {
        AnalyticManager.SetProperty(propertyName, propertyValue);
    }

    public static void LogEvent(GameEvent gameEvent) {
        AnalyticEvent analyticEvent = AnalyticEvent.Create(gameEvent.Name);

        foreach (var param in gameEvent.Paramaters) {
            analyticEvent.Add(param.Key, param.Value.GetAnalyticParamter());
        }

        ReferencePool.Release(gameEvent);

        AnalyticManager.LogEvent(analyticEvent);
    }

    public static void LogAdRevenue(GameAdRevenue gameRevenue) {
        AnalyticAdRevenue analyticRevenue = new AnalyticAdRevenue(gameRevenue.AdPlatform, gameRevenue.AdSource, gameRevenue.AdUnit, gameRevenue.AdFormat, gameRevenue.Value, gameRevenue.CurrencyCode);

        AnalyticManager.LogAdRevenue(analyticRevenue);

    }

    public static void LogIAPRevenue(GameIAPRevenue gameRevenue) {
        AnalyticIAPRevenue analyticRevenue = new AnalyticIAPRevenue(gameRevenue.ProductId, gameRevenue.Quantity, gameRevenue.Value, gameRevenue.CurrencyCode);

        AnalyticManager.LogIAPRevenue(analyticRevenue);
    }
}
