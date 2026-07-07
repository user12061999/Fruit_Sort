using HAVIGAME;
using HAVIGAME.Scenes;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using HAVIGAME.Services.Advertisings;
using HAVIGAME.Plugins.AppsFlyer;

public class LogoSceneController : SceneController {
    [SerializeField] private LoadingView loadingView;
    [SerializeField] private PoolPreloader[] poolPreloaders;
    [SerializeField] private GameObject ingameConsolePrefab;

    private void Awake() {
        DOTween.SetTweensCapacity(512, 128);

        loadingView.Initialize();

        DontDestroyOnLoad(this.gameObject);
    }

    protected override void OnSceneStart() {
        GameManager.Instance.ManualInitialize();

        StartCoroutine(IEFakeLoading());
    }

    private IEnumerator IEFakeLoading() {
        loadingView.OnStart();
        yield return StartCoroutine(loadingView.FadeOut());
        yield return StartCoroutine(loadingView.OnStartLoading());
        yield return StartCoroutine(loadingView.FadeIn());

        if (Log.LogLevel == LogLevel.Debug) {
            if (ingameConsolePrefab) Instantiate(ingameConsolePrefab);
        }

        int steps = 1;
        int totalSteps = 5;
        float elapsed = 0f;
        float duration = GameRemoteConfig.AppOpenLoadingDuration;
        float loadingProcessing = 0f;
        Log.Debug("[Boostrap] Start initialize");
        float startTime = Time.time;
        Log.Debug($"[Boostrap] Initialize game launcher {Time.time - startTime}/{duration}s");
        while (steps == 1) {
            elapsed += Time.deltaTime;

            if (!GameLauncher.Launching) {
                loadingProcessing = Mathf.Clamp(loadingProcessing + Time.deltaTime, 0, Mathf.Min((float)steps / totalSteps, elapsed / duration));
                loadingView.OnLoading(loadingProcessing);
                yield return null;
            }
            else {
                steps++;
            }
        }
        
        GameData.Initialize();
        GameAdvertising.Instance.Create();
        HeartRegenerator.Instance.Create();
        yield return null;

        Log.Debug($"[Boostrap] Initialize game data {Time.time - startTime}/{duration}s");
        
        int seasonCount = GameData.Player.SeasonCount;

        foreach (var item in poolPreloaders) {
            elapsed += Time.deltaTime;
            loadingProcessing = Mathf.Clamp(loadingProcessing + Time.deltaTime, 0, Mathf.Min((float)steps / totalSteps, elapsed / duration));
            loadingView.OnLoading(loadingProcessing);
            item.Preload();
            yield return null;
        }

        steps++;

        if (GameAdvertising.IsRemoveAds() || (!GameAdvertising.IsAppOpenIntertitialAdEnable && !GameAdvertising.IsAppOpenAdEnable)) {
            LoadOtherAds(LoadType.Custom_1);
            steps += 2;
        } else {
#if !ADVERTISING
            steps += 2;
#endif
        }
        
        Log.Debug($"[Boostrap] Initialize game advertising {Time.time - startTime}/{duration}s");
        
        while (steps == 3) {
            if (seasonCount > 0) elapsed += Time.deltaTime;

            if (!GameAdvertising.IsAllServiceInitialized()) {
                loadingProcessing = Mathf.Clamp(loadingProcessing + Time.deltaTime, 0, Mathf.Min((float)steps / totalSteps, elapsed / duration));
                loadingView.OnLoading(loadingProcessing);

                if (elapsed >= duration) {
                    steps++;
                }

                yield return null;
            } else {
                steps++;
            }
        }

        GameAnalytics.SetProperty("media_source", AppsFlyerManager.MediaSource);
        GameAnalytics.SetProperty("campaign", AppsFlyerManager.Campaign);
        GameAnalytics.SetProperty("adset", AppsFlyerManager.AdSet);

        bool useAppOpenInterstitialAd = seasonCount <= 0 && GameAdvertising.IsAppOpenIntertitialAdEnable;
        InterstitialAd appOpenInterstitialAd = null;
        if (steps == 4) {
            if (useAppOpenInterstitialAd) {
                appOpenInterstitialAd = AdvertisingManager.GetInterstitialAd(GameAdvertising.appOpenInterstitialAdFilter);

                if (appOpenInterstitialAd != null) {
                    appOpenInterstitialAd.SetAutoLoad(LoadType.OnLoadFailed);
                    appOpenInterstitialAd.Load();
                } else {
                    useAppOpenInterstitialAd = false;
                    LoadAppOpenAds(LoadType.Custom_1);
                }
            } else {
                LoadAppOpenAds(LoadType.Custom_1);
            }
        } else {
            LoadOtherAds(LoadType.Custom_1);
        }

        while (steps == 4) {
            elapsed += Time.deltaTime;

            if (!GameAdvertising.IsInitialized() || (useAppOpenInterstitialAd ? !GameAdvertising.IsAppOpenInterstitialAdReady : !GameAdvertising.IsAppOpenAdReady)) {
                loadingProcessing = Mathf.Clamp(loadingProcessing + Time.deltaTime, 0, Mathf.Min((float)steps / totalSteps, elapsed / duration));
                loadingView.OnLoading(loadingProcessing);

                if (elapsed >= duration) {
                    LoadOtherAds(LoadType.Custom_1);
                    Log.Debug($"[Boostrap] Load app open ads time out {Time.time - startTime}/{duration}s");
                    steps++;
                }

                yield return null;
            }
            else {
                LoadOtherAds(LoadType.Custom_1);
                Log.Debug($"[Boostrap] Load app open ads complete {Time.time - startTime}/{duration}s");
                steps++;
            }
        }

        bool completed = true;

#if UNITY_EDITOR

        if (useAppOpenInterstitialAd) {
            completed = !GameAdvertising.TryShowAppOpenInterstitialAd(() => completed = true);
        }
        else {
            completed = !GameAdvertising.TryShowAppOpenAd(() => completed = true);
        }

#else
        completed = true;
        if (useAppOpenInterstitialAd) {
            GameAdvertising.TryShowAppOpenInterstitialAd();
        }
        else {
            GameAdvertising.TryShowAppOpenAd();
        }
#endif


        while (!completed) {
            yield return null;
        }

        //MissionManager.Instance.Create();

        GameData.Player.OnStartGameSeason();

        AsyncOperation operation = SceneManager.LoadSceneAsync(GameScene.ByIndex.Home, LoadSceneMode.Single);

        while (!operation.isDone) {

            elapsed += Time.deltaTime;

            loadingProcessing = Mathf.Clamp(loadingProcessing + Time.deltaTime, 0, Mathf.Min((float)steps / totalSteps, elapsed / duration));
            loadingView.OnLoading(loadingProcessing);

            if (operation.progress >= ScenesManager.maxProgress) {
                break;
            }
            else {
                yield return null;
            }
        }

        while (elapsed < duration) {
            elapsed += 10 * Time.deltaTime;
            loadingView.OnLoading(elapsed / duration);
            yield return null;
        }

        yield return StartCoroutine(loadingView.FadeOut());
        LoadRewardedAds(LoadType.Custom_1);
        operation.allowSceneActivation = true;
        yield return new WaitUntil(() => operation.isDone);

        yield return StartCoroutine(loadingView.OnFinishLoading());

        yield return StartCoroutine(loadingView.FadeIn());

        loadingView.OnFinish();

        Destroy(this.gameObject);
    }

private void LoadAppOpenAds(LoadType loadType) {
        foreach (var item in AdvertisingManager.GetAppOpenAds(GameAdvertising.allAdFilter)) {
            if (!item.IsLoading && item.HasAutoLoad(loadType)) {
                item.Load();
            }
        }
    }

    private void LoadRewardedAds(LoadType loadType) {
        foreach (var item in AdvertisingManager.GetRewardedAds(GameAdvertising.allAdFilter)) {
            if (!item.IsLoading && item.HasAutoLoad(loadType)) {
                item.Load();
            }
        }
        foreach (var item in AdvertisingManager.GetRewardedInterstitialAds(GameAdvertising.allAdFilter)) {
            if (!item.IsLoading && item.HasAutoLoad(loadType)) {
                item.Load();
            }
        }
    }

    private void LoadOtherAds(LoadType loadType) {
        foreach (var item in AdvertisingManager.GetInterstitialAds(GameAdvertising.allAdFilter)) {
            if (!item.IsLoading && item.HasAutoLoad(loadType)) {
                item.Load();
            }
        }
        foreach (var item in AdvertisingManager.GetBannerAds(GameAdvertising.allAdFilter)) {
            if (!item.IsLoading && item.HasAutoLoad(loadType)) {
                item.Load();
            }
        }
        foreach (var item in AdvertisingManager.GetMediumRectangleAds(GameAdvertising.allAdFilter)) {
            if (!item.IsLoading && item.HasAutoLoad(loadType)) {
                item.Load();
            }
        }
        foreach (var item in AdvertisingManager.GetNativeAds(GameAdvertising.allAdFilter)) {
            if (!item.IsLoading && item.HasAutoLoad(loadType)) {
                item.Load();
            }
        }
        foreach (var item in AdvertisingManager.GetNativeImmersiveAds(GameAdvertising.allAdFilter)) {
            if (!item.IsLoading && item.HasAutoLoad(loadType)) {
                item.Load();
            }
        }
        foreach (var item in AdvertisingManager.GetNativeOverlayAds(GameAdvertising.allAdFilter)) {
            if (!item.IsLoading && item.HasAutoLoad(loadType)) {
                item.Load();
            }
        }
    }
    [System.Serializable]
    private class PoolPreloader {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int capacity = 4;

        public void Preload() {
            GameObjectPool.Instance.CreatePool(prefab, capacity);
        }
    }
}
