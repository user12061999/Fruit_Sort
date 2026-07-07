using DG.Tweening;
using System;

public class Timer {
    private Tween tween;
    private int total;
    private int elapsed;
    private int remaining;

    public bool IsRunning => tween != null;
    public bool IsPaused => tween != null && !tween.IsPlaying();
    public int Total => total;
    public int Elapsed => elapsed;
    public int Remaining => remaining;


    public void Reset() {
        elapsed = 0;
        remaining = 0;
        total = 0;
    }

    public void Pause() {
        tween.Pause();
    }

    public void Resume() {
        tween.Play();
    }

    public void Stop() {
        tween?.Kill();
        tween = null;
    }

    public void Complete() {
        tween?.Kill(true);
        tween = null;
    }

    public void Countdown(int duration, Action onUpdate = null, Action onCompleted = null, bool ignoreTimeScale = false) {
        total = duration;
        remaining = duration;
        elapsed = 0;

        tween?.Kill();

        onUpdate?.Invoke();

        tween = DOVirtual.DelayedCall(1, Empty, ignoreTimeScale)
                         .SetLoops(duration)
                         .SetRecyclable(true)
                         .OnStepComplete(() => {
                             elapsed++;
                             remaining--;
                             onUpdate?.Invoke();
                         })
                         .OnComplete(() => {
                             tween = null;
                             onCompleted?.Invoke();
                         });
    }

    private void Empty() { }

}
