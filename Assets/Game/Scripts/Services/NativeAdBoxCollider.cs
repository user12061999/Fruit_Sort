using DG.Tweening;
using HAVIGAME;
using HAVIGAME.Plugins.AppsFlyer;
using HAVIGAME.Services.Advertisings;
using UnityEngine;

public class NativeAdBoxCollider : MonoBehaviour {
    [SerializeField, ButtonField("Update", "UpdateSize", 60)] private BoxCollider boxCollider;
    [SerializeField] private IntProperty boxScaleX = IntProperty.Create();
    [SerializeField] private IntProperty boxScaleY = IntProperty.Create();
    [SerializeField] private IntProperty boxScaleDelay = IntProperty.Create();

    private RectTransform rectTransform;
    private Canvas canvas;
    private Rect rect;
    private Tween delayTween;
    private bool scaleRequire;
    private bool scaled;

    private void Awake() {
        canvas = GetComponentInParent<Canvas>();
        rectTransform = transform as RectTransform;
        rect = RectTransformUtility.PixelAdjustRect(rectTransform, canvas);
        scaleRequire = true;
        scaled = false;
    }

    private void Start() {
        AdvertisingManager.onAdClicked += AdvertisingManager_onAdClicked;
    }

    private void OnDestroy() {
        AdvertisingManager.onAdClicked -= AdvertisingManager_onAdClicked;
    }

    private void AdvertisingManager_onAdClicked(AdEventArgs args) {
        if (args.AdFormat == AdFormat.NativeAd) {
            scaleRequire = true;
            UpdateBoxSize();
        }
    }

    private void OnEnable() {
        UpdateBoxSize();
    }

    private void UpdateSize() {
        if (boxCollider == null) return;

        canvas = GetComponentInParent<Canvas>();
        rectTransform = transform as RectTransform;
        rect = RectTransformUtility.PixelAdjustRect(rectTransform, canvas);

        float width = rect.width;
        float height = rect.height;

        boxCollider.size = new Vector3(width, height, 1);

        Vector2 pivot = rectTransform.pivot;
        Vector2 center = new Vector3((0.5f - pivot.x) * width, (0.5f - pivot.y) * height, 0);

        boxCollider.center = center;
    }

    public void UpdateBoxSize() {
        bool isOrganic = AppsFlyerManager.IsOrganic;

        if (isOrganic) {
            UpdateBoxSizeWithoutScale();
        } else {
            if (!scaleRequire) {
                if (scaled) {
                    UpdateBoxSizeWithScale();
                } else {
                    UpdateBoxSizeWithoutScale();
                }
            } else {
                UpdateBoxSizeWithoutScale();

                delayTween?.Kill();
                delayTween = null;
                scaleRequire = false;

                float scaleDelay = boxScaleDelay.Get();

                Log.Debug(Utility.Text.Format("[NativeAdBoxCollider] Delay scale = {0} seconds", scaleDelay));

                if (scaleDelay > 0) {
                    delayTween = DOVirtual.DelayedCall(scaleDelay, UpdateBoxSizeWithScale)
                        .SetRecyclable(true)
                        .SetUpdate(true)
                        .OnComplete(() => delayTween = null);

                    Log.Debug(Utility.Text.Format("[NativeAdBoxCollider] Delay scale box size atfer {0} seconds", scaleDelay));
                }
            }
        }
    }

    public void UpdateBoxSizeWithoutScale() {
        if (boxCollider == null) return;

        scaled = false;
        float width = rect.width;
        float height = rect.height;

        boxCollider.size = new Vector3(width, height, 1);

        Vector2 pivot = rectTransform.pivot;
        Vector2 center = new Vector3((0.5f - pivot.x) * width, (0.5f - pivot.y) * height, 0);

        boxCollider.center = center;

        Log.Debug(Utility.Text.Format("[NativeAdBoxCollider] Update box size without scale, size = {0}", boxCollider.size));
    }

    public void UpdateBoxSizeWithScale() {
        if (boxCollider == null) return;

        scaled = true;
        float width = rect.width;
        float height = rect.height;

        float scaleX = boxScaleX.Get() / 100f;
        float scaleY = boxScaleY.Get() / 100f;

        float widthScaled = width * scaleX;
        float heightScaled = height * scaleY;

        boxCollider.size = new Vector3(widthScaled, heightScaled, 1);

        Vector2 pivot = rectTransform.pivot;
        Vector2 center = new Vector3((0.5f - pivot.x) * widthScaled, (0.5f - pivot.y) * heightScaled, 0);

        boxCollider.center = center;

        Log.Debug(Utility.Text.Format("[NativeAdBoxCollider] Update box size scale, size = {0}", boxCollider.size));
    }
}
