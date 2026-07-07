namespace HAVIGAME.Services.Privacy {
    public interface IPrivacyService {
        public InitializeEvent InitializeEvent { get; }
        public bool IsInitialized { get; }
        public void Initialize();
        public void RequestAuthorization(System.Action<AuthorizationStatus> onCompleted);
    }

    [System.Serializable]
    public abstract class PrivacyServiceProvider : ServiceProvider<IPrivacyService> {

    }
}