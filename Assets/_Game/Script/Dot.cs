using UnityEngine;

/// <summary>
/// Represents a single dot/pixel in the game.
/// Has color ID, HP, and tracks its current state (InGrid / Falling / OnBelt).
/// </summary>
public class Dot : MonoBehaviour
{
    // --- Public fields (Inspector-configurable) ---
    [Header("Dot Properties")]
    public int colorID;           // Color identifier for matching with buckets
    public int maxHP = 3;         // Default HP
    public int currentHP;         // Current HP (set to maxHP on Awake)
    public float dotSize = 0.3f;  // Visual/logical size of this dot

    [Header("State")]
    public DotState state = DotState.InGrid;

    [Header("Grid Info")]
    public int gridX;             // Column in the pixel grid
    public int gridY;             // Row in the pixel grid

    [Header("Movement")]
    public float lateralOffset;   // Offset from spline center (clamped to belt width)
    public float splineProgress;  // 0..1 along the conveyor spline
    public float moveSpeed = 1f;  // Speed multiplier along spline
    public float rotationSpeed;   // Random rotation speed

    // Visual
    private SpriteRenderer sr;
    private Color dotColor;

    /// <summary>
    /// Enum for dot lifecycle states.
    /// </summary>
    public enum DotState
    {
        InGrid,    // Still part of the pixel grid (can be shot)
        Falling,   // Breaking free, falling toward the conveyor
        OnBelt     // Riding the conveyor spline
    }

    private void Awake()
    {
        currentHP = maxHP;
        sr = GetComponent<SpriteRenderer>();
        rotationSpeed = Random.Range(-90f, 90f);
    }

    /// <summary>
    /// Apply damage to this dot. If HP reaches 0, triggers break/fall.
    /// </summary>
    public void TakeDamage(int dmg)
    {
        if (state != DotState.InGrid && state != DotState.Falling) return;

        currentHP -= dmg;
        currentHP = Mathf.Max(0, currentHP);

        // Flash white on hit
        if (sr != null)
        {
            sr.color = Color.white;
            Invoke(nameof(RestoreColor), 0.08f);
        }

        if (currentHP <= 0)
        {
            OnBreak();
        }
    }

    /// <summary>
    /// Called when HP reaches 0 — transitions to Falling state
    /// and registers with FallingPixelManager.
    /// </summary>
    private void OnBreak()
    {
        state = DotState.Falling;
        lateralOffset = Random.Range(-0.4f, 0.4f); // Random lateral position on belt

        // Notify FallingPixelManager
        if (FallingPixelManager.Instance != null)
        {
            FallingPixelManager.Instance.AddDot(this);
        }
    }

    /// <summary>
    /// Restore the dot's color after a hit flash.
    /// </summary>
    private void RestoreColor()
    {
        if (sr != null)
            sr.color = dotColor;
    }

    /// <summary>
    /// Set the visual color of this dot (called by grid manager on spawn).
    /// </summary>
    public void SetColor(Color c)
    {
        dotColor = c;
        if (sr != null)
            sr.color = c;
    }
}
