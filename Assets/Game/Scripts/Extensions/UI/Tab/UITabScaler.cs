using DG.Tweening;
using HAVIGAME.UI;
using UnityEngine;

public class UITabScaler : UITabExtension {
    [SerializeField] private RectTransform target;
    [SerializeField] private float anchorScale = 1.2f;
    [SerializeField] private Vector2 sizeDeltaOffset = new Vector2(0, 50);
    [SerializeField] private Vector2 anchoredPositionOffset = new Vector2(0, 0);

    private UITabManager manager;
    private Sequence animTween;

    public override void Initialize(UITabManager manager) {
        this.manager = manager;

        manager.onFrameShowed += OnFrameShowed;

        if (manager.Current != null) {
            ScaleTo(manager.Current.transform, true);
        }
    }

    private void OnFrameShowed(UIFrame frame) {
        if (frame == null) return;

        ScaleTo(manager.GetButtonOf((UITab)frame).transform, false);
    }

    private void ScaleTo(Transform child, bool instant) {
        int childCount = target.childCount;
        int childIndex = GetChildIndex(target, child);

        animTween?.Kill();
        animTween = null;

        float normalRange = 1f / childCount;
        float activeRange = normalRange * anchorScale;
        float deactiveRange = (1f - activeRange) / (childCount - 1);

        float currentRange = 0f;

        if (instant) {
            for (int i = 0; i < target.childCount; i++) {
                Vector2 anchorMin = new Vector2(currentRange, 0);
                Vector2 anchorMax = new Vector2(currentRange + (i == childIndex ? activeRange : deactiveRange), 1);
                Vector2 anchoredPosition = i == childIndex ? anchoredPositionOffset : Vector2.zero;
                Vector2 sizeDelta = i == childIndex ? sizeDeltaOffset : Vector2.zero;

                SetPosition(target.GetChild(i), anchorMin, anchorMax, anchoredPosition, sizeDelta);

                currentRange += i == childIndex ? activeRange : deactiveRange;
            }
        } else {
            animTween = DOTween.Sequence();

            for (int i = 0; i < target.childCount; i++) {
                Vector2 anchorMin = new Vector2(currentRange, 0);
                Vector2 anchorMax = new Vector2(currentRange + (i == childIndex ? activeRange : deactiveRange), 1);
                Vector2 anchoredPosition = i == childIndex ? anchoredPositionOffset : Vector2.zero;
                Vector2 sizeDelta = i == childIndex ? sizeDeltaOffset : Vector2.zero;

                AddAnimation(animTween, target.GetChild(i), anchorMin, anchorMax, anchoredPosition, sizeDelta);

                currentRange += i == childIndex ? activeRange : deactiveRange;
            }

            animTween.SetRecyclable(true);
            animTween.OnComplete(() => {
                animTween = null;
            });
        }
    }

    private void AddAnimation(Sequence sequence, Transform transform, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta) {
        RectTransform rect = transform as RectTransform;

        sequence.Join(rect.DOAnchorMin(anchorMin, 0.25f));
        sequence.Join(rect.DOAnchorMax(anchorMax, 0.25f));
        sequence.Join(rect.DOAnchorPos(anchoredPosition, 0.25f));
        sequence.Join(rect.DOSizeDelta(sizeDelta, 0.25f));

    }

    private void SetPosition(Transform transform, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta) {
        RectTransform rect = transform as RectTransform;

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private int GetChildIndex(Transform parent, Transform child) {
        for (int i = 0; i < parent.childCount; i++) {
            if (parent.GetChild(i) == child) return i;
        }

        return -1;
    }
}