using System;
using HAVIGAME.Services.Auth;

#if GOOGLE_PLAY_GAMES
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace HAVIGAME.Plugins.GooglePlayGame {
    public class GooglePlayGameAuth : IAuthService {
        public readonly InitializeEvent initializeEvent = new InitializeEvent();

        public InitializeEvent InitializeEvent => initializeEvent;
        public bool IsInitialized => initializeEvent.IsInitialized;

        public bool IsSignedIn {
            get {
#if GOOGLE_PLAY_GAMES
                return PlayGamesPlatform.Instance.IsAuthenticated();
#else
                return false;
#endif
            }
        }

        public AuthNetwork Network => AuthNetwork.Google;

        public void Initialize() {
            if (initializeEvent.IsRunning) {
                Log.Warning("[GooglePlayGameAuth] Google Play Games Auth is running with initialize state {0}.", IsInitialized);
                return;
            }

            GooglePlayGameManager.initializeEvent.AddListener(OnGooglePlayGameInitialized);
        }

        private void OnGooglePlayGameInitialized(bool isInitialized) {
            if (isInitialized) {
                Log.Info("[GooglePlayGameAuth] Initialize completed.");
                initializeEvent.Invoke(true);
            }
            else {
                Log.Error("[GooglePlayGameAuth] Initialize failed because GooglePlayGames initialize failed.");
                initializeEvent.Invoke(false);
            }
        }

        public string GetUserId() {
#if GOOGLE_PLAY_GAMES
            if (IsSignedIn) {
                return PlayGamesPlatform.Instance.GetUserId();
            }
            else {
                return null;
            }
#else
            return null;
#endif
        }

        public string GetUserName() {
#if GOOGLE_PLAY_GAMES
            if (IsSignedIn) {
                return PlayGamesPlatform.Instance.GetUserDisplayName();
            }
            else {
                return null;
            }
#else
            return null;
#endif
        }

        public void SignIn(Action<bool> onCompleted) {
            if (!IsInitialized) {
                Log.Warning("[GooglePlayGameAuth] GooglePlayGameAuth no initialize!");
                onCompleted?.Invoke(false);
                return;
            }

#if GOOGLE_PLAY_GAMES
            PlayGamesPlatform.Instance.Authenticate(status => {
                if (status == SignInStatus.Success) {
                    Log.Info("[GooglePlayGame] Authenticate with Google Play Games successful.");
                }
                else {
                    Log.Error("[GooglePlayGame] Authenticate with Google Play Games failed.");
                }
            });
#else
            onCompleted?.Invoke(false);
#endif
        }
    }

    [CategoryMenu("Google Play Games")]
    [System.Serializable]
    public class GooglePlayGameAuthServiceProvider : AuthServiceProvider {
        public override IAuthService GetService() {
#if FACEBOOK
            return new GooglePlayGameAuth();
#else
            return null;
#endif
        }
    }
}
