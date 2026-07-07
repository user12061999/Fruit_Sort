using HAVIGAME;
using System;
using UnityEngine;

public class LevelTimer : MonoBehaviour
{
    [SerializeField] private bool ignoreTimeScale = true;

    private Timer timer = new Timer();

    private Action onCompleted;

    public bool IsRunning => timer.IsRunning;
    public bool IsPaused => timer.IsPaused;
    public int TotalSeconds => timer.Total;
    public int ElapsedSeconds => timer.Elapsed;
    public int RemainingSeconds => timer.Remaining;

    public void Pause()
    {
        timer.Pause();
    }

    public void Resume()
    {
        timer.Resume();
    }

    public void Stop()
    {
        timer.Stop();
    }

    public void Add(int seconds)
    {
        if (IsRunning)
        {
            int remainingSeconds = RemainingSeconds;
            int totalSeconds = remainingSeconds + seconds;

            Stop();

            if (totalSeconds > 0)
            {
                timer.Countdown(totalSeconds, OnCountdownUpdated, OnCountdownCompleted, ignoreTimeScale);
                Pause();
            }
            else
            {
                OnCountdownCompleted();
            }
        }
    }
    public void AddTime(int seconds)
    {

        int remainingSeconds = RemainingSeconds;
        int totalSeconds = remainingSeconds + seconds;

        Stop();

        if (totalSeconds > 0)
        {
            timer.Countdown(totalSeconds, OnCountdownUpdated, OnCountdownCompleted, ignoreTimeScale);
            Pause();
        }
        else
        {
            OnCountdownCompleted();
        }

    }

    public void Countdown(int totalSeconds, Action onCompleted)
    {

        this.onCompleted = onCompleted;

        timer.Stop();
        timer.Countdown(totalSeconds, OnCountdownUpdated, OnCountdownCompleted, ignoreTimeScale);
    }

    private void OnCountdownUpdated()
    {
        EventDispatcher.Dispatch(new GameEvent.LevelTimeChanged(TotalSeconds, ElapsedSeconds, RemainingSeconds));
    }

    private void OnCountdownCompleted()
    {
        onCompleted?.Invoke();
    }
}
