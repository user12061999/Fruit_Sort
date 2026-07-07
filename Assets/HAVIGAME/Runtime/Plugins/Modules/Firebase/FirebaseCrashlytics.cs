using HAVIGAME.Services.Crashlytics;

namespace HAVIGAME.Plugins.Firebases {
#if FIREBASE && FIREBASE_CRASHLYTICS
    public class FirebaseCrashlytics : ICrashlyticService {

        public InitializeEvent InitializeEvent => FirebaseManager.initializeEvent;

        public bool IsInitialized => FirebaseManager.IsInitialized;

        public void Initialize() {
            Log.Debug("[FirebaseCrashlytics] Firebase crashlytics will initialize with Firebase.");
        }

        public void LogMessage(string message) {
            if (!IsInitialized) {
                Log.Warning("[FirebaseCrashlytics] Firebase no initialize.");
                return;
            }

            Firebase.Crashlytics.Crashlytics.Log(message);
        }


        public void LogException(System.Exception exception) {
            if (!IsInitialized) {
                Log.Warning("[FirebaseCrashlytics] Firebase no initialize.");
                return;
            }

            Firebase.Crashlytics.Crashlytics.LogException(exception);
        }
    }
#endif


    [CategoryMenu("Firebase Crashlytics")]
    [System.Serializable]
    public class FirebaseCrashlyticServiceProvider : CrashlyticServiceProvider {
        public override ICrashlyticService GetService() {
#if FIREBASE && FIREBASE_CRASHLYTICS
            return new FirebaseCrashlytics();
#else
            return null;
#endif
        }
    }
}
