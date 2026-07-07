using UnityEngine;

public class ProgressBarInt : ProgressBar<int> {
    public override float GetPercent(int currentValue, int maxValue) {
        return (float)currentValue / maxValue;
    }

    public override int Lerp(int currentValue, int maxValue, float time) {
        return Mathf.CeilToInt(Mathf.Lerp(currentValue, maxValue, time));
    }
}
