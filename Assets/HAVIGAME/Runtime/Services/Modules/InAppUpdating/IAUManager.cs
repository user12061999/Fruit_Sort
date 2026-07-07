using System.Collections;
using System;
using UnityEngine;

#if IAU
using Google.Play.Common;
using Google.Play.AppUpdate;
#endif

namespace HAVIGAME.Services.IAU {
    public static class IAUManager {
        public const string DEFINE_SYMBOL = "IAU";

        public static readonly InitializeEvent initializeEvent = new InitializeEvent();
#if IAU
        private static UpdateManager updateManager;
#endif

        public static bool IsInitialized => initializeEvent.IsInitialized;

        public static void Initialize() {
#if IAU

            IAUSettings settings = IAUSettings.Instance;

            if (initializeEvent.IsRunning) {
                Log.Warning("[IAUManager] Cancel initialize! In-App Updating initialized with result {0}", initializeEvent.IsInitialized);
                return;
            }

#if UNITY_EDITOR
            Log.Error("[IAUManager] Initialize the in-app Updating failed! In-App Updating no support on Unity Editor.");
            initializeEvent.Invoke(false);
#else

            updateManager = new UpdateManager(settings.UpdateMode, settings.AllowAssetPackDeletion);

            Log.Info("[IAUManager] Initialize the in-app Updating completed!");
            initializeEvent.Invoke(true);
#endif

#endif
        }

        public static bool CheckForUpdate(Action<UpdateHandle> onCompleted, Action<string> onFailed) {
#if IAU
            if (!IsInitialized) {
                Log.Warning("[IAUManager] Check failed! In-App Updating is not initialized.");
                return false;
            }

            updateManager.CheckForUpdate(onCompleted, onFailed);
            return true;
#else
            onFailed?.Invoke("[IAUManager] Check failed! In-App Updating is not enabled.");
            return false;
#endif
        }


#if IAU
        public class UpdateManager {
            private AppUpdateManager appUpdateManager;
            private UpdateMode updateMode;
            private bool allowAssetPackDeletion;
            private Action<UpdateHandle> onCompleted;
            private Action<string> onFailed;

            public UpdateManager(UpdateMode updateMode, bool allowAssetPackDeletion) {
                this.updateMode = updateMode;
                this.allowAssetPackDeletion = allowAssetPackDeletion;
                this.onCompleted = null;
                this.onFailed = null;

                appUpdateManager = new AppUpdateManager();
            }

            public void CheckForUpdate(Action<UpdateHandle> onCompleted, Action<string> onFailed) {
                this.onCompleted = onCompleted;
                this.onFailed = onFailed;
                Executor.Instance.Run(IECheckForUpdate());
            }

            private IEnumerator IECheckForUpdate() {
                PlayAsyncOperation<AppUpdateInfo, AppUpdateErrorCode> appUpdateInfoOperation = appUpdateManager.GetAppUpdateInfo();

                yield return appUpdateInfoOperation;

                if (appUpdateInfoOperation.IsSuccessful) {
                    AppUpdateInfo appUpdateInfoResult = appUpdateInfoOperation.GetResult();

                    if (appUpdateInfoResult != null) {
                        Log.Debug(Utility.Text.Format("Check for update completed! update info = {0}", appUpdateInfoResult));
                        UpdateHandle handle = new UpdateHandle(appUpdateManager, appUpdateInfoResult, updateMode, allowAssetPackDeletion, appUpdateInfoResult.UpdatePriority, (UpdateAvailability)appUpdateInfoResult.UpdateAvailability, appUpdateInfoResult.AvailableVersionCode);
                        onCompleted?.Invoke(handle);
                    }
                    else {
                        onFailed?.Invoke("Check for update failed! Getting update info is null.");
                    }
                }
                else {
                    onFailed?.Invoke(Utility.Text.Format("Check for update failed with error = {0}.", appUpdateInfoOperation.Error));
                }
            }
        }
#endif

        public class UpdateHandle {
#if IAU
            private UpdateMode updateMode;
            private AppUpdateManager appUpdateManager;
            private AppUpdateInfo appUpdateInfo;
            private AppUpdateOptions appUpdateOptions;
#endif

            public int UpdatePriority { get;private set; }
            public UpdateAvailability UpdateAvailability { get; private set; }
            public int AvailableVersionCode { get; private set; }
            public float DownloadProgress { get; private set; }
            public ulong BytesDownloaded { get; private set; }
            public ulong TotalBytesToDownload { get; private set; }

#if IAU
            public UpdateHandle(AppUpdateManager appUpdateManager, AppUpdateInfo appUpdateInfo, UpdateMode updateMode, bool allowAssetPackDeletion, int updatePriority, UpdateAvailability updateAvailability, int availableVersionCode) {
                this.appUpdateManager = appUpdateManager;
                this.appUpdateInfo = appUpdateInfo;
                this.updateMode = updateMode;
                this.UpdatePriority = updatePriority;
                this.UpdateAvailability = updateAvailability;
                this.AvailableVersionCode = availableVersionCode;
                this.DownloadProgress = 0;
                this.BytesDownloaded = 0;
                this.TotalBytesToDownload = 0;

                switch (updateMode) {
                    case UpdateMode.Immediate:
                        appUpdateOptions = AppUpdateOptions.ImmediateAppUpdateOptions(allowAssetPackDeletion);
                        break;
                    case UpdateMode.Flexible:
                        appUpdateOptions = AppUpdateOptions.FlexibleAppUpdateOptions(allowAssetPackDeletion);
                        break;
                }
            }
#endif

            public void StartUpdate(Action<bool> callback) {
#if IAU
                Executor.Instance.Run(IEStartUpdate(callback));
#else
                callback?.Invoke(false);

#endif
            }

#if IAU
            private IEnumerator IEStartUpdate(Action<bool> callback) {
                AppUpdateRequest startUpdateRequest = appUpdateManager.StartUpdate(appUpdateInfo, appUpdateOptions);

                while (!startUpdateRequest.IsDone) {
                    DownloadProgress = startUpdateRequest.DownloadProgress;
                    BytesDownloaded = startUpdateRequest.BytesDownloaded;
                    TotalBytesToDownload = startUpdateRequest.TotalBytesToDownload;
                    yield return null;
                }

                if (startUpdateRequest.Error != AppUpdateErrorCode.NoError) {
                    if (updateMode == UpdateMode.Flexible) {
                        PlayAsyncOperation<VoidResult, AppUpdateErrorCode> completeUpdateRequest = appUpdateManager.CompleteUpdate();
                        yield return completeUpdateRequest;

                        if (completeUpdateRequest.IsSuccessful) {
                            Log.Info("Update completed! App is restarting...");
                            callback?.Invoke(true);
                        }
                        else {
                            Log.Error(Utility.Text.Format("Update failed with error = {0}.", completeUpdateRequest.Error));
                            callback?.Invoke(false);
                        }
                    }
                    else {
                        Log.Info("Update completed! App is restarting...");
                        callback?.Invoke(true);
                    }
                }
                else {
                    Log.Error(Utility.Text.Format("Update failed with error = {0}.", startUpdateRequest.Error));
                    callback?.Invoke(false);
                }
            }
#endif
        }

        public enum UpdateAvailability {
            Unknown = 0,
            UpdateNotAvailable = 1,
            UpdateAvailable = 2,
            DeveloperTriggeredUpdateInProgress = 3
        }

#if IAU
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {

            public override int Order => SERVICE - 10;
            public override InitializeEvent InitializeEvent => IAUManager.initializeEvent;

            public override void Initialize() {
                IAUManager.Initialize();
            }
        }
#endif
    }

}
