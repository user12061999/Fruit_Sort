using System;
using HAVIGAME.Services.Auth;

#if FACEBOOK
using Facebook.Unity;
#endif

namespace HAVIGAME.Plugins.Facebook {
    public class FacebookAuth : IAuthService {
        public readonly InitializeEvent initializeEvent = new InitializeEvent();

        public InitializeEvent InitializeEvent => initializeEvent;
        public bool IsInitialized => initializeEvent.IsInitialized;

        public bool IsSignedIn {
            get {
#if FACEBOOK
                return FB.IsLoggedIn;
#else
                return false;
#endif
            }
        }

        public AuthNetwork Network => AuthNetwork.Facebook;

        public void Initialize() {
            if (initializeEvent.IsRunning) {
                Log.Warning("[FacebookAuth] Facebook Auth is running with initialize state {0}.", IsInitialized);
                return;
            }

            FacebookManager.initializeEvent.AddListener(OnFacebookInitialized);
        }

        private void OnFacebookInitialized(bool isInitialized) {
            if (isInitialized) {
                Log.Info("[FacebookAuth] Initialize completed.");
                initializeEvent.Invoke(true);
            }
            else {
                Log.Error("[FacebookAuth] Initialize failed because Facebook initialize failed.");
                initializeEvent.Invoke(false);
            }
        }

        public string GetUserId() {
#if FACEBOOK
            if (IsSignedIn) {
                return FB.CurrentProfile().UserID;
            }
            else {
                return null;
            }
#else
            return null;
#endif
        }

        public string GetUserName() {
#if FACEBOOK
            if (IsSignedIn) {
                return FB.CurrentProfile().Name;
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
                Log.Warning("[FacebookAuth] FacebookAuth no initialize!");
                onCompleted?.Invoke(false);
                return;
            }

#if FACEBOOK

#if UNITY_ANDROID
            ExpressLogin(completed => {
                if (completed) {
                    onCompleted?.Invoke(true);
                }
                else {
                    BasicLogin(onCompleted);
                }
            });
#else
            BasicLogin(onCompleted);
#endif

#else
            onCompleted?.Invoke(false);
#endif
        }

#if FACEBOOK
        private void ExpressLogin(Action<bool> onCompleted) {
            FB.Android.RetrieveLoginStatus(result => {
                if (!string.IsNullOrEmpty(result.Error)) {
                    Log.Info(Utility.Text.Format("[FacebookAuth] Express login error, error = {0}.", result.Error));
                    onCompleted?.Invoke(false);
                }
                else if (result.Failed) {
                    Log.Info("[FacebookAuth] Express login failed, access token could not be retrieved");
                    onCompleted?.Invoke(false);
                }
                else {
                    AccessToken accessToken = result.AccessToken;

                    foreach (var item in accessToken.Permissions) {
                        Log.Info(Utility.Text.Format("[FacebookAuth] Granted permissions: {0}", item));
                    }

                    Log.Info("[FacebookAuth] Express login completed.");
                    onCompleted?.Invoke(true);
                }
            });
        }

        private void BasicLogin(Action<bool> onCompleted) {
            FB.LogInWithReadPermissions(FacebooksSettings.Instance.Permissions, result => {
                if (FB.IsLoggedIn) {
                    AccessToken accessToken = result.AccessToken;

                    foreach (var item in accessToken.Permissions) {
                        Log.Info(Utility.Text.Format("[FacebookAuth] Granted permissions: {0}", item));
                    }

                    Log.Info("[FacebookAuth] Login completed.");
                    onCompleted?.Invoke(true);
                }
                else {
                    Log.Info("[FacebookAuth] Login failed. User cancelled login.");
                    onCompleted?.Invoke(false);
                }
            });
        }
#endif
    }

    [CategoryMenu("Facebook")]
    [System.Serializable]
    public class FacebookAuthServiceProvider : AuthServiceProvider {
        public override IAuthService GetService() {
#if FACEBOOK
            return new FacebookAuth();
#else
            return null;
#endif
        }
    }
}
