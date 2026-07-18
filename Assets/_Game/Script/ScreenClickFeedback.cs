using UnityEngine;

namespace FruitSort
{
    /// <summary>Phát feedback khi người chơi click vùng gameplay, không áp dụng cho UI.</summary>
    public sealed class ScreenClickFeedback : MonoBehaviour
    {
        void Update()
        {
            if (GameplayPause.IsPaused || !PointerInput.PressedThisFrame() ||
                UIPointerGuard.IsPointerOverUI())
                return;

            Camera camera = Camera.main;
            if (camera == null || !PointerInput.TryGetPosition(out Vector2 pointerPosition)) return;

            Vector3 screenPosition = pointerPosition;
            screenPosition.z = Mathf.Abs(camera.transform.position.z);
            Vector3 worldPosition = camera.ScreenToWorldPoint(screenPosition);
            worldPosition.z = 0f;

            if (EffectManager.Instance != null)
                EffectManager.Instance.Play(GameEffectType.ScreenClick, worldPosition);
        }
    }
}
