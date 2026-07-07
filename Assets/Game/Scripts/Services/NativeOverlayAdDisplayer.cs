using HAVIGAME.Services.Advertisings;
using HAVIGAME;
using UnityEngine;
using System.Collections;
using System;

public class NativeOverlayAdDisplayer : AdDisplayer {
    [SerializeField] private Canvas canvas;
    [SerializeField] private NativeOverlayAdStyle adStyle = new NativeOverlayAdStyle();
    [SerializeField] private AdSizeOption adSize = new AdSizeOption(AdSizeType.Banner);
    [SerializeField] private AdPositions position = AdPositions.BottomCenter;
    [SerializeField] private Vector2Int offset = Vector2Int.zero;
    [SerializeField] private bool autoShow = true;
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private IntProperty refreshTime = IntProperty.Create();

    private NativeOverlayAd cacheNativeAd;
    private NativeOverlayAd nativeAd;
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
        if (IsReady) {

            if (cacheNativeAd != null) {
                cacheNativeAd.SetOwner(null);
                cacheNativeAd.Destroy();
                cacheNativeAd = null;
            }

            if (canvas != null) {
                Vector2 canvasSize = (canvas.transform as RectTransform).sizeDelta;
                Vector2 screenSize = new Vector2(Screen.width, Screen.height);
                float horizontalScale = canvasSize.x / screenSize.x;
                float verticalScale = canvasSize.y / screenSize.y;
                float scale = Mathf.Min(horizontalScale, verticalScale);
                AdSizeOption scaledAdSize = adSize.Scale(scale, scale);
                NativeOverlayAdStyle scaledAdStyle = adStyle.Scale(scale);
                Vector2Int scaledOffset = new Vector2Int(Mathf.FloorToInt(offset.x * scale), Mathf.FloorToInt(offset.y * scale));

                nativeAd.Show(position, scaledOffset, scaledAdStyle, scaledAdSize, placement);

            } else {
                nativeAd.Show(position, offset, adStyle, adSize, placement);
            }
            if (autoRefresh) {
                refreshCoroutine = StartCoroutine(IERefreshAd());
            }
        } else {
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

        if (GameAdvertising.TryGetNativeOverlayAd(filter, out nativeAd)) {
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
