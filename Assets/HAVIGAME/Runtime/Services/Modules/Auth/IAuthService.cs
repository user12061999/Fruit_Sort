namespace HAVIGAME.Services.Auth {

    public interface IAuthService {
        public InitializeEvent InitializeEvent { get; }
        public bool IsInitialized { get; }
        public bool IsSignedIn { get; }
        public AuthNetwork Network { get; }
        public void Initialize();
        public string GetUserId();
        public string GetUserName();
        public void SignIn(System.Action<bool> onCompleted);
    }

    [System.Serializable]
    public enum AuthNetwork : byte {
        None,
        Google,
        Facebook,
    }

    [System.Serializable]
    public abstract class AuthServiceProvider : ServiceProvider<IAuthService> {

    }
}
