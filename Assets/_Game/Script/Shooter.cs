using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class Shooter : MonoBehaviour
{
    [Header("Shot")]
    public int damage = 1;
    [Min(0.01f)] public float fireInterval = 0.15f;
    [Min(0.1f)] public float maxDistance = 30f;
    public LayerMask dotLayerMask = ~0;
    public bool allowHoldToFire = true;
    public bool readPlayerInput = true;

    [Header("Optional Visual")]
    public LineRenderer shotLine;
    [Min(0f)] public float shotLineDuration = 0.05f;

    private readonly RaycastHit2D[] hits = new RaycastHit2D[64];
    private float nextFireTime;
    private float hideLineTime;

    private void Update()
    {
        if (readPlayerInput && ReadFireInput())
        {
            Fire();
        }

        if (shotLine != null && shotLine.enabled && Time.time >= hideLineTime)
        {
            shotLine.enabled = false;
        }
    }

    public void Fire()
    {
        if (Time.time < nextFireTime)
        {
            return;
        }

        nextFireTime = Time.time + fireInterval;
        Vector2 origin = transform.position;
        Vector2 direction = transform.up;
        int hitCount = Physics2D.RaycastNonAlloc(origin, direction, hits, maxDistance, dotLayerMask);
        Dot closestDot = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Dot dot = hits[i].collider != null ? hits[i].collider.GetComponentInParent<Dot>() : null;
            if (dot != null && dot.state == DotState.InGrid && hits[i].distance < closestDistance)
            {
                closestDot = dot;
                closestDistance = hits[i].distance;
            }
        }

        closestDot?.TakeDamage(damage);
        ShowShotLine(origin, direction, closestDot != null ? closestDistance : maxDistance);
    }

    private bool ReadFireInput()
    {
#if ENABLE_INPUT_SYSTEM
        bool pressed = Mouse.current != null &&
            (allowHoldToFire ? Mouse.current.leftButton.isPressed : Mouse.current.leftButton.wasPressedThisFrame);
        pressed |= Keyboard.current != null &&
            (allowHoldToFire ? Keyboard.current.spaceKey.isPressed : Keyboard.current.spaceKey.wasPressedThisFrame);
        return pressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return allowHoldToFire ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1");
#else
        return false;
#endif
    }

    private void ShowShotLine(Vector2 origin, Vector2 direction, float distance)
    {
        if (shotLine == null)
        {
            return;
        }

        shotLine.positionCount = 2;
        shotLine.SetPosition(0, origin);
        shotLine.SetPosition(1, origin + direction * distance);
        shotLine.enabled = true;
        hideLineTime = Time.time + shotLineDuration;
    }
}
