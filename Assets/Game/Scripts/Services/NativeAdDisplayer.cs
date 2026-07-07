using System;
using System.Collections;
using UnityEngine;
using HAVIGAME;
using HAVIGAME.Services.Advertisings;

public class NativeAdDisplayer : AdDisplayer {
    [SerializeField] protected NativeAdElementView[] elementViews;
    [SerializeField] private Transform root;
    [SerializeField] private bool autoShow = true;
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private IntProperty refreshTime = IntProperty.Create();

    private NativeAdView view;
    private NativeAd cacheNativeAd;
    private NativeAd nativeAd;
    private Coroutine createCoroutine;
    private Coroutine loadCoroutine;
    private Coroutine requestCoroutine;
    private Coroutine refreshCoroutine;
    private float targetRefreshTime;

    public bool AutoShow => autoShow;
    public bool AutoRefresh => autoRefresh;
    public bool IsRunning { get; private set; }
    public bool IsCreating => createCoroutine != null;
    public bool IsLoading => loadCoroutine != null;
    public bool IsRequesting => requestCoroutine != null;
    public bool IsRefreshing => refreshCoroutine != null;
    public bool IsShowing {
        get {
#if UNITY_EDITOR
            return root.gameObject.activeInHierarchy;
#else
            return root.gameObject.activeInHierarchy && ((nativeAd != null && nativeAd.IsShowing) || (cacheNativeAd != null && cacheNativeAd.IsShowing));
#endif
        }
    }
    public bool IsAvaliable => nativeAd != null;
    public bool IsReady {
        get {
#if UNITY_EDITOR || CHEAT
            return true;
#else
            return IsAvaliable && nativeAd.IsReady;
#endif
        }
    }
    public int RefreshTime => refreshTime.Get();

    protected override void Awake() {
        base.Awake();

        view = new NativeAdView(elementViews);
        root.gameObject.SetActive(false);
    }

    private void OnEnable() {
        if (autoShow) Show();
    }

    private void OnDisable() {
        Hide();
    }

    protected override void OnDestroy() {
        base.OnDestroy();

        Destroy();
    }

    public override void Show() {
        if (IsRunning) {
            Log.Warning("[NativeAdDisplayer] Show failed! Native ad is running!");
            return;
        }

        IsRunning = true;

        if (nativeAd == null) {
            createCoroutine = StartCoroutine(IECreateAd(() => StartCoroutine(IELoadAd(RequestAd))));
        } else if (!IsReady) {
            StartCoroutine(IELoadAd(RequestAd));
        } else {
            RequestAd();
        }
    }


    private void Refresh() {
        if (nativeAd == null) {
            createCoroutine = StartCoroutine(IECreateAd(() => StartCoroutine(IELoadAd(RequestAd))));
        } else {
            StartCoroutine(IELoadAd(RequestAd));
        }
    }

    public override void Hide() {
        if (!IsRunning) {
            Log.Warning("[NativeAdDisplayer] Hide failed! Native ad is not running!");
            return;
        }

        IsRunning = false;

        StopCreateAd();
        StopLoadAd();
        StopRequestAd();
        StopRefreshAd();

        if (nativeAd != null && nativeAd.IsShowing) {
            nativeAd.Hide();
        }

        if (cacheNativeAd != null && cacheNativeAd.IsShowing) {
            cacheNativeAd.Hide();
        }

        root.gameObject.SetActive(false);
    }

    public override void Destroy() {
        IsRunning = false;

        StopCreateAd();
        StopLoadAd();
        StopRequestAd();
        StopRefreshAd();

        if (nativeAd != null) {
            nativeAd.SetOwner(null);
            nativeAd.Destroy();
            nativeAd = null;
        }

        if (cacheNativeAd != null) {
            cacheNativeAd.SetOwner(null);
            cacheNativeAd.Destroy();
            cacheNativeAd = null;
        }
    }

    private void RequestAd() {
        root.gameObject.SetActive(false);

#if UNITY_EDITOR
        if (root) root.gameObject.SetActive(true);

        if (autoRefresh) {
            refreshCoroutine = StartCoroutine(IERefreshAd());
        }
#else
        if (IsReady) {
            if (cacheNativeAd != null) {
                cacheNativeAd.SetOwner(null);
                cacheNativeAd.Destroy();
                cacheNativeAd = null;
            }

            root.gameObject.SetActive(true);
            nativeAd.Show(view, placement);

            if (autoRefresh) {
                refreshCoroutine = StartCoroutine(IERefreshAd());
            }
        } else {
            root.gameObject.SetActive(false);
            requestCoroutine = StartCoroutine(IERequestAd());
        }
#endif
    }

    private void StopCreateAd() {
        if (IsCreating) StopCoroutine(createCoroutine);
    }

    private void StopLoadAd() {
        if (IsLoading) StopCoroutine(loadCoroutine);
    }

    private void StopRequestAd() {
        if (IsRequesting) StopCoroutine(requestCoroutine);
    }

    private void StopRefreshAd() {
        if (IsRefreshing) StopCoroutine(refreshCoroutine);
    }

    private IEnumerator IECreateAd(Action onCompleted) {
        WaitForSeconds wait = Executor.Instance.WaitForSeconds(1);

        while (!GameAdvertising.IsInitialized() || !GameAdvertising.IsAllServiceInitialized()) {
            yield return wait;
        }

        if (GameAdvertising.TryGetNativeAd(filter, out nativeAd)) {
            nativeAd.SetOwner(this);
            createCoroutine = null;
            onCompleted?.Invoke();
        } else {
            createCoroutine = null;
        }
    }

    private IEnumerator IELoadAd(Action onCompleted) {
        if (!nativeAd.IsLoading) nativeAd.Load();

        WaitForSeconds wait = Executor.Instance.WaitForSeconds(1);

        while (!IsReady) {
            yield return wait;
        }

        targetRefreshTime = Time.time + RefreshTime;

        onCompleted?.Invoke();
    }

    private IEnumerator IERequestAd() {
        WaitForSeconds wait = Executor.Instance.WaitForSeconds(1);

        while (!IsReady) {
            yield return wait;
        }

        requestCoroutine = null;
        RequestAd();
    }

    private IEnumerator IERefreshAd() {
        float delayTime = Mathf.Max(1, targetRefreshTime - Time.time);
        Log.Info($"Refresh ad in {delayTime}s");
        yield return Executor.Instance.WaitForSeconds(delayTime);
        refreshCoroutine = null;

        if (cacheNativeAd != null) {
            cacheNativeAd.SetOwner(null);
            cacheNativeAd.Destroy();
            cacheNativeAd = null;
        }

        cacheNativeAd = nativeAd;
        nativeAd = null;
        Refresh();
    }
}
