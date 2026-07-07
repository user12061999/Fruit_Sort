using UnityEngine;
using TMPro;
using DG.Tweening;
using HAVIGAME;

public class NotifyPopup : MonoBehaviour {
    public static readonly Vector2 showAnchorPosition = new Vector2(0, -150);
    public static readonly Vector2 hideAnchorPosition = new Vector2(0, 150);

    [SerializeField] private TextMeshProUGUI txtMessage;

    private Sequence sequence;

    public void Show(string message) {
        txtMessage.text = message;

        sequence?.Kill();
        sequence = DOTween.Sequence();

        RectTransform rectTransform = transform as RectTransform;
        rectTransform.anchoredPosition = hideAnchorPosition;

        sequence.Append(rectTransform.DOAnchorPos(showAnchorPosition, 0.5f).SetEase(Ease.OutBack))
                .AppendInterval(1.5f)
                .Append(rectTransform.DOAnchorPos(hideAnchorPosition, 0.5f).SetEase(Ease.InBack))
                .OnComplete(() => {
                    sequence = null;
                    this.Recycle();
                });
    }
}
