using UnityEngine;

namespace FruitSort
{
    /// <summary>
    /// Cờ pause gameplay toàn cục khi popup UI đè lên GamePanel.
    /// KHÔNG dùng Time.timeScale (nó kéo theo anim UI phải chạy unscaled, dễ sót);
    /// từng hệ gameplay tự check cờ này: FallingPixelManager (di chuyển dot),
    /// ModelDotSpawner/Bucket (input + coroutine spawn/nhả), freeze booster (đếm giây).
    /// ClassicLevelController.PauseGameplay/ResumeGameplay là nơi DUY NHẤT set cờ.
    /// </summary>
    public static class GameplayPause
    {
        public static bool IsPaused { get; private set; }

        public static void Set(bool paused)
        {
            IsPaused = paused;
        }

        // Domain reload có thể tắt trong editor -> reset tay khi vào Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad()
        {
            IsPaused = false;
        }
    }
}
