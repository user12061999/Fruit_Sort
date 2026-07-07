using HAVIGAME;
using HAVIGAME.Services.Advertisings;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NativeImmersiveAdDisplayer : AdDisplayer {
    [SerializeField] private AdAspectRatio adAspect = new AdAspectRatio(AdAspectRatioType.AR_1x1);
    [SerializeField] private bool canvasMode = true;
    [SerializeField] private bool clickable = true;
    [SerializeField] private bool adBadgeEnable = true;
    [SerializeField] private float scale = 1;
    [SerializeField] private List<GameObject> friendlyObjects;
    [SerializeField] private Transform root;
    [SerializeField] private bool autoShow = true;
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private IntProperty refreshTime = IntProperty.Create();

    private NativeImmersiveAd cacheNativeAd;
    private NativeImmersiveAd nativeAd;
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
    public bool IsShowing => (nativeAd != null && nativeAd.IsShowing) || (cacheNativeAd != null && cacheNativeAd.IsShowing);
    public bool IsAvaliable => nativeAd != null;
    public bool IsReady {
        get {
            return IsAvaliable && nativeAd.IsReady;
        }
    }
    public int RefreshTime => refreshTime.Get();

    protected override void Awake() {
        base.Awake();

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

    public void Refresh() {
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

        if (IsReady) {

            if (cacheNativeAd != null) {
                cacheNativeAd.SetOwner(null);
                cacheNativeAd.Destroy();
                cacheNativeAd = null;
            }

            root.gameObject.SetActive(true);
            nativeAd.SetPosition(root, Vector3.zero, Quaternion.identity, Vector3.one * scale);

            if (nativeAd.CanvasMode) {
                nativeAd.SetPosition(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0.5f, 0.5f));
            }

            nativeAd.Show(placement);

            if (autoRefresh) {
                refreshCoroutine = StartCoroutine(IERefreshAd());
            }
        } else {
            root.gameObject.SetActive(false);
            requestCoroutine = StartCoroutine(IERequestAd());
        }
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

        if (GameAdvertising.TryGetNativeImmersiveAd(filter, out nativeAd)) {
            nativeAd.SetOwner(this);
            createCoroutine = null;
            onCompleted?.Invoke();
        } else {
            createCoroutine = null;
        }
    }

    private IEnumerator IELoadAd(Action onCompleted) {
        nativeAd.SetAspectRatio(adAspect);
        nativeAd.SetCanvasMode(canvasMode);
        nativeAd.SetClickable(clickable);
        nativeAd.SetAdBadge(adBadgeEnable);
        nativeAd.SetFriendlyObjects(friendlyObjects);

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
