using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class FixUIBanner : MonoBehaviour {
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private bool fixBannerAnchorPosition = true;
    [SerializeField, Range(0f, 1f)] private float bannerAnchorPositionScale = 0.5f;
    [SerializeField] private bool fixMRectAnchorPosition = true;
    [SerializeField, Range(0f, 1f)] private float mrectAnchorPositionScale = 0.5f;
    [SerializeField] private bool fixReferenceResolution = true;
    [SerializeField, Range(0f, 1f)] private float referenceResolutionScale = 0.5f;
    [SerializeField] private RectTransform topPlacement;
    [SerializeField] private RectTransform bottomPlacement;
    [SerializeField, Range(0f, 1f)] private float topExpand;
    [SerializeField, Range(0f, 1f)] private float bottomExpand;

    private CanvasScaler canvasScaler;
    private RectTransform rectTransform;
    private Coroutine coroutine;

    private Vector2 defaultReferenceResolution;

    private void Awake() {
        rectTransform = GetComponent<RectTransform>();
        canvasScaler = GetComponentInParent<CanvasScaler>();

        if (canvasScaler != null) {
            defaultReferenceResolution = canvasScaler.referenceResolution;
        }
    }

    private void OnEnable() {
        if (coroutine != null) StopCoroutine(coroutine);

        if (autoRefresh) {
            coroutine = StartCoroutine(IEFix(0.25f, 3f));
        } else {
            FixUI();
        }
    }

    private IEnumerator IEFix(float delay, float replace) {
        yield return new WaitForSeconds(delay);
        FixUI();

        WaitForSeconds wait = new WaitForSeconds(replace);

        while (true) {
            yield return wait;
            FixUI();
        }
    }

    private void FixUI() {
        FixSafeArea();
    }

    private void FixSafeArea() {
        Rect safeArea = Screen.safeArea;
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= screenWidth;
        anchorMin.y /= screenHeight;
        anchorMax.x /= screenWidth;
        anchorMax.y /= screenHeight;

        float top = Mathf.Max(fixBannerAnchorPosition ? GetBannerHeightOnTop() * bannerAnchorPositionScale : 0, fixMRectAnchorPosition ? GetMRectHeightOnTop() * mrectAnchorPositionScale : 0);
        float bottom = Mathf.Max(fixBannerAnchorPosition ? GetBannerHeightOnBottom() * bannerAnchorPositionScale : 0, fixMRectAnchorPosition ? GetMRectHeightOnBottom() * mrectAnchorPositionScale : 0);

        if (top > 0) top *= (1 + topExpand);
        if (bottom > 0) bottom *= (1 + bottomExpand);

        anchorMin.y += bottom / screenHeight;
        anchorMax.y -= top / screenHeight;

        rectTransform.anchorMin = anchorMin;
        if(GameController.HasInstance)
        {
            anchorMax.y = 1;
        }
        rectTransform.anchorMax = anchorMax;
        rectTransform.sizeDelta = Vector2.zero;

        if (topPlacement) {
            topPlacement.sizeDelta = new Vector2(0, top);
        }

        if (bottomPlacement) {
            bottomPlacement.sizeDelta = new Vector2(0, bottom);
        }

        if (fixReferenceResolution && canvasScaler != null) {
            canvasScaler.referenceResolution = new Vector2(defaultReferenceResolution.x, defaultReferenceResolution.y + ((top + bottom) * referenceResolutionScale));
        }
    }

    private float GetBannerHeightOnTop() {
        switch (GameAdvertising.GetBannerAdPosition()) {
            case GameAdvertising.GameAdPosition.TopCenter:
            case GameAdvertising.GameAdPosition.TopLeft:
            case GameAdvertising.GameAdPosition.TopRight:
                try {
                    return GameAdvertising.GetBannerHeight();
                }
                catch {
                    if (GameAdvertising.IsShowingBannerAd) {
                        return 336f;
                    }
                    return 0;
                }
            default: return 0;
        }
    }

    private float GetBannerHeightOnBottom() {
        if (GameAdvertising.IsShowingBannerAd) {
            switch (GameAdvertising.GetBannerAdPosition()) {
                case GameAdvertising.GameAdPosition.BottomCenter:
                case GameAdvertising.GameAdPosition.BottomLeft:
                case GameAdvertising.GameAdPosition.BottomRight:
                    try {
                        return GameAdvertising.GetBannerHeight();
                    } catch {
                        if (GameAdvertising.IsShowingBannerAd) {
                            return 336f;
                        }
                        return 0;
                    }
                default: return 0;
            }
        } else {
            return GetNativeHeightOnBottom();
        }
    }

    private float GetNativeHeightOnBottom() {
        /*if (OverlayCanvas.HasInstance) {
            return OverlayCanvas.Instance.GetNativeHeightOnBottom();
        } else {
            return 0;
        }*/
        return 0;
    }

    private float GetMRectHeightOnTop() {
        switch (GameAdvertising.GetMRectAdPosition()) {
            case GameAdvertising.GameAdPosition.TopCenter:
            case GameAdvertising.GameAdPosition.TopLeft:
            case GameAdvertising.GameAdPosition.TopRight:
                try {
                    return GameAdvertising.GetMRectHeight();
                }
                catch {
                    if (GameAdvertising.IsShowingBannerAd) {
                        return 500f;
                    }
                    return 0;
                }
            default: return 0;
        }
    }

    private float GetMRectHeightOnBottom() {
        switch (GameAdvertising.GetMRectAdPosition()) {
            case GameAdvertising.GameAdPosition.BottomCenter:
            case GameAdvertising.GameAdPosition.BottomLeft:
            case GameAdvertising.GameAdPosition.BottomRight:
                try {
                    return GameAdvertising.GetMRectHeight();
                }
                catch {
                    if (GameAdvertising.IsShowingBannerAd) {
                        return 500f;
                    }
                    return 0;
                }
            default: return 0;
        }
    }
}
