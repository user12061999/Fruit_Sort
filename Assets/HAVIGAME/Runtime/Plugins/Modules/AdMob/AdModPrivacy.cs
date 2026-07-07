using HAVIGAME.Services.Privacy;
using System;
using UnityEngine;

#if ADMOB
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#endif

namespace HAVIGAME.Plugins.AdMob {
    public class AdModPrivacy : IPrivacyService {
        public readonly InitializeEvent initializeEvent = new InitializeEvent();

#if ADMOB
        private AdMobConsentController consentController;
#endif

        public InitializeEvent InitializeEvent => initializeEvent;

        public bool IsInitialized => InitializeEvent.IsInitialized;

        public void Initialize() {
#if ADMOB
            if (InitializeEvent.IsRunning) {
                Log.Warning("[AdModPrivacy] AdMob privacy is running with initialize state {0}.", IsInitialized);
                return;
            }
            
            AdMobSettings settings = AdMobSettings.Instance;
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            consentController = new AdMobConsentController(settings.TagForUnderAgeOfConsent == Result.True);

            Log.Info("[AdModPrivacy] AdMob privacy initialize completed.");
            InitializeEvent.Invoke(true);
#endif
        }

        public void RequestAuthorization(Action<AuthorizationStatus> onCompleted) {
            if (!IsInitialized) {
                Log.Warning("[AdModPrivacy] AdMod privacy no initialize!");
                return;
            }

#if ADMOB
            consentController.Request(onCompleted);
#endif
        }

#if ADMOB
        private class AdMobConsentController {
            private bool tagForUnderAgeOfConsent;
            private bool requested;
            private bool isRequesting;
            private float timeScaleCache;
            private AuthorizationStatus status;
            private Action<AuthorizationStatus> onCompleted;

            public bool Requested => requested;
            public bool IsRequesting => isRequesting;
            public AuthorizationStatus Status => status;

            public AdMobConsentController(bool tagForUnderAgeOfConsent) {
                this.tagForUnderAgeOfConsent = tagForUnderAgeOfConsent;
                status = AuthorizationStatus.Unknown;
                isRequesting = false;
                requested = false;
                onCompleted = null;
            }

            public void Request(Action<AuthorizationStatus> onCompleted) {
                if (Requested) {
                    onCompleted?.Invoke(Status);
                    return;
                }

                this.onCompleted = onCompleted;
                this.timeScaleCache = Time.timeScale;
                Time.timeScale = 0;
                ConsentRequestParameters requestParameters = new ConsentRequestParameters();
                requestParameters.TagForUnderAgeOfConsent = tagForUnderAgeOfConsent;

                ConsentInformation.Update(requestParameters, OnConsentInfoUpdated);
            }


            private void OnConsentInfoUpdated(FormError error) {
                if (error != null) {
                    Log.Error("Consent infomation update failed with error code {0}: {1}.", error.ErrorCode, error.Message);
                    onCompleted?.Invoke(AuthorizationStatus.Unknown);
                    Time.timeScale = timeScaleCache;
                    isRequesting = false;
                }
                else {
                    Log.Debug("Consent infomation update completed with status {0}.", ConsentInformation.ConsentStatus);

                    LoadAndShowConsentForm();
                }
            }

            private void LoadAndShowConsentForm() {
                Log.Debug("Load and show consent form.");

                ConsentForm.LoadAndShowConsentFormIfRequired(OnConsentFormLoadAndShowCallback);
            }

            private void OnConsentFormLoadAndShowCallback(FormError error) {
                if (error != null) {
                    Log.Error("Consent form load failed with error code {0}: {1}.", error.ErrorCode, error.Message);
                    onCompleted?.Invoke(AuthorizationStatus.Unknown);
                    Time.timeScale = timeScaleCache;
                    isRequesting = false;
                }
                else {
                    Log.Debug("Consent form show completed.");
                    onCompleted?.Invoke(AuthorizationStatus.Unknown);
                    Time.timeScale = timeScaleCache;
                    isRequesting = false;
                }
            }
        }
#endif
    }

    [CategoryMenu("AdMob Privacy")]
    [System.Serializable]
    public class AdMobPrivacyServiceProvider : PrivacyServiceProvider {
        public override IPrivacyService GetService() {
#if ADMOB
            return new AdModPrivacy();
#else
            return null;
#endif
        }
    }
}