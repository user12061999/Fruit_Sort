using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public abstract class ProgressBar<T> : MonoBehaviour {
    [SerializeField] private Image imgBar;
    [SerializeField] private TextMeshProUGUI txtValue;
    [SerializeField] private string valueFormat = "{0:#,##0}/{1:#,##0}";

    private Coroutine coroutine;

    private T currentValue;

    public void SetProgress(T value, T maxValue) {
        this.currentValue = value;
        float percent = GetPercent(value, maxValue);
        if (imgBar) imgBar.fillAmount = percent;
        if (imgBar) txtValue.text = string.Format(valueFormat, value, maxValue);
    }

    public void ChangeProgress(T value, T maxValue) {
        if (coroutine != null) StopCoroutine(coroutine);

        coroutine = StartCoroutine(IESetProgress(value, maxValue));
    }

    private IEnumerator IESetProgress(T value, T maxValue) {
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration) {
            yield return null;
            elapsed += Time.deltaTime;
            T result = Lerp(currentValue, value, elapsed / duration);
            SetProgress(result, maxValue);
        }

        SetProgress(value, maxValue);
    }

    public abstract float GetPercent(T currentValue, T maxValue);

    public abstract T Lerp(T currentValue, T maxValue, float time);
}
