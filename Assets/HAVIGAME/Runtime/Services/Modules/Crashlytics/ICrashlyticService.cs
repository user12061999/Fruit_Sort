namespace HAVIGAME.Services.Crashlytics {
    public interface ICrashlyticService {
        public InitializeEvent InitializeEvent { get; }
        public bool IsInitialized { get; }
        public void Initialize();
        public void LogMessage(string message);
        public void LogException(System.Exception exception);
    }


    [System.Serializable]
    public abstract class CrashlyticServiceProvider : ServiceProvider<ICrashlyticService> {

    }
}