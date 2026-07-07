namespace HAVIGAME.Services.Analytics {

    public interface IAnalyticService {
        public InitializeEvent InitializeEvent { get; }
        public bool IsInitialized { get; }
        public void Initialize();
        public void SetProperty(string propertyName, string propertValue);
        public void LogEvent(AnalyticEvent analyticEvent);
        public void LogAdRevenue(AnalyticAdRevenue analyticRevenue);
        public void LogIAPRevenue(AnalyticIAPRevenue analyticRevenue);
    }

    [System.Serializable]
    public abstract class AnalyticServiceProvider : ServiceProvider<IAnalyticService> {

    }
}
