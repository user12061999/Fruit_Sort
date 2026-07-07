using UnityEngine;
using DG.Tweening;
using HAVIGAME.UI;

[RequireComponent(typeof(UITabManager))]
public class UITabScroller : UITabExtension {
    [SerializeField] private RectTransform target;

    private RectTransform canvasRect;
    private UITabManager manager;
    private Tween animTween;

    public override void Initialize(UITabManager manager) {
        this.manager = manager;
        this.canvasRect = transform.root.GetComponentInChildren<Canvas>().transform as RectTransform;

        manager.onFrameShowed += OnFrameShowed;

        if (manager.Current != null) {
            ScrollTo(manager.Current.transform, true);
        }
    }

    private void OnFrameShowed(UIFrame frame) {
        if (frame == null) return;

        ScrollTo(frame.transform, false);
    }

    private void ScrollTo(Transform child, bool instant) {
        int childCount = target.childCount;
        int childIndex = GetChildIndex(target, child);

        float screenWidth = canvasRect.sizeDelta.x;

        Vector2 targetAnchorPos = new Vector2(-childIndex * screenWidth, 0);

        animTween?.Kill();
        animTween = null;

        if (instant) {
            target.anchoredPosition = targetAnchorPos;
        } else {
            animTween = target.DOAnchorPos(targetAnchorPos, 0.25f)
                .SetRecyclable(true)
                .OnComplete(() => {
                    animTween = null;
                });
        }
    }

    private int GetChildIndex(Transform parent, Transform child) {
        for (int i = 0; i < parent.childCount; i++) {
            if (parent.GetChild(i) == child) return i;
        }

        return -1;
    }
}