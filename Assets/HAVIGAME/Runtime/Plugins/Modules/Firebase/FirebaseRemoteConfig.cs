using System;
using System.Collections.Generic;
using HAVIGAME.Services.RemoteConfig;

#if FIREBASE && FIREBASE_REMOTE_CONFIG
using Firebase.Extensions;
#endif

namespace HAVIGAME.Plugins.Firebases {

#if FIREBASE && FIREBASE_REMOTE_CONFIG
    public class FirebaseRemoteConfig : IRemoteConfigService {

        public readonly InitializeEvent initializeEvent = new InitializeEvent();

        public InitializeEvent InitializeEvent => initializeEvent;
        public bool IsInitialized => initializeEvent.IsInitialized;

        public int ValueCount {
            get {
                if (IsInitialized) {
                    return Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.AllValues.Count;
                }
                return 0;
            }
        }

        public IEnumerable<KeyValuePair<string, string>> AllValues {
            get {
                if (IsInitialized) {
                    foreach (var item in Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.AllValues) {
                        yield return new KeyValuePair<string, string>(item.Key, item.Value.StringValue);
                    }
                }
                else {
                    yield break;
                }
            }
        }

        public void Initialize() {
            if (initializeEvent.IsRunning) {
                Log.Warning("[FirebaseRemoteConfig] Firebase Remote Config is running with initialize state {0}.", IsInitialized);
                return;
            }

            FirebaseManager.initializeEvent.AddListener(OnFirebaseInitialized);

        }

        private void OnFirebaseInitialized(bool isInitialized) {
            if (isInitialized) {
                FirebaseSettings settings = FirebaseSettings.Instance;

                if (settings.FetchAndActiveOnInitialize) {

                    Log.Debug("[FirebaseRemoteConfig] Start fetch and active remote config values.");

                    TimeSpan delayTime = new TimeSpan(0, 1, 0);
                    Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.FetchAsync(delayTime).ContinueWithOnMainThread(fetchTask => {
                        if (fetchTask.IsCompleted) {
                            Log.Debug("[FirebaseRemoteConfig] Fetch completed.");

                            Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.ActivateAsync().ContinueWithOnMainThread(activateTask => {
                                if (activateTask.IsCompleted) {
                                    Log.Debug("[FirebaseRemoteConfig] Activate completed.");
                                    Log.Info("[FirebaseRemoteConfig] Initialize completed.");
                                    initializeEvent.Invoke(true);
                                }
                                else {
                                    Log.Error("[FirebaseRemoteConfig] Activate failed.");
                                    Log.Info("[FirebaseRemoteConfig] Initialize completed.");
                                    initializeEvent.Invoke(true);
                                }
                            });
                        }
                        else {
                            Log.Error("[FirebaseRemoteConfig] Fetch failed.");
                            Log.Info("[FirebaseRemoteConfig] Initialize completed.");
                            initializeEvent.Invoke(true);
                        }
                    });
                }
                else {
                    Log.Info("[FirebaseRemoteConfig] Initialize completed.");
                    initializeEvent.Invoke(true);
                }
                
                Database.Unload(settings);
            }
            else {
                Log.Error("[FirebaseRemoteConfig] Initialize failed because Firebase initialize failed.");
                initializeEvent.Invoke(false);
            }
        }
    }
#endif

    [CategoryMenu("Firebase Remote Config")]
    [System.Serializable]
    public class FirebaseRemoteConfigServiceProvider : RemoteConfigServiceProvider {
        public override IRemoteConfigService GetService() {
#if FIREBASE && FIREBASE_REMOTE_CONFIG
            return new FirebaseRemoteConfig();
#else
            return null;
#endif
        }
    }
}

