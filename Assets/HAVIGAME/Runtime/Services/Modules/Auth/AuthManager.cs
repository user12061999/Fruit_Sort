using UnityEngine;

namespace HAVIGAME.Services.Auth {
    public static class AuthManager {
        public const string DEFINE_SYMBOL = "AUTH";

        public static readonly InitializeEvent initializeEvent = new InitializeEvent();

        private static IAuthService authService;

        public static bool IsInitialized => initializeEvent.IsInitialized;
        public static bool IsSignedIn => IsInitialized && authService.IsSignedIn;

        public static string UserId {
            get {
                if (IsInitialized) {
                    return authService.GetUserId();
                }
                else {
                    return null;
                }
            }
        }

        public static string UserName {
            get {
                if (IsInitialized) {
                    return authService.GetUserName();
                }
                else {
                    return null;
                }
            }
        }

        public static void Initialize() {
#if AUTH
            if (initializeEvent.IsRunning) {
                Log.Warning("[Auth] Cancel initialize! Auth is running initialize state {0}.", IsInitialized);
                return;
            }

            AuthSettings settings = AuthSettings.Instance;

            authService = settings.GetServiceProvider().GetService();
            authService.InitializeEvent.AddListener(OnAuthServiceInitialized);
            authService.Initialize();

            //Database.Unload(settings);
#endif
        }

        private static void OnAuthServiceInitialized(bool isInitialized) {
            if (isInitialized) {
                Log.Info("[Auth] Initialize completed.");
                initializeEvent.Invoke(true);

                if (AuthSettings.Instance.AutoAuth) {
                    authService.SignIn(result => {
                        if (result) {
                            Log.Info("[Auth] Sign-in completed.");
                        }
                        else {
                            Log.Error("[Auth] Sign-in failed.");
                        }
                    });
                }
            }
            else {
                Log.Error("[Auth] Initialize failed.");
                initializeEvent.Invoke(false);
            }
        }

        public static void SignIn(System.Action<bool> onCompleted) {
            if (!IsInitialized) {
                Log.Warning("[Auth] Auth no initialize!");
                onCompleted?.Invoke(false);
            }

            authService.SignIn(onCompleted);
        }

#if AUTH
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {

            public override int Order => SERVICE;
            public override InitializeEvent InitializeEvent => AuthManager.initializeEvent;

            public override void Initialize() {
                AuthManager.Initialize();
            }
        }
#endif
    }
}
