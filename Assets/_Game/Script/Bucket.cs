using UnityEngine;

/// <summary>
/// A bucket placed along the conveyor belt.
/// Attracts dots of matching colorID when they pass within attractRadius.
/// When currentFill reaches maxFill, the bucket is full and gets destroyed.
/// </summary>
public class Bucket : MonoBehaviour
{
    [Header("Bucket Properties")]
    public int colorID;                  // Which color this bucket accepts
    public int maxFill = 5;              // Number of dots needed to fill
    public int currentFill;              // Current fill count
    public float attractRadius = 0.8f;   // Radius to attract matching dots
    public float attractSpeed = 4f;      // Speed at which dots are pulled in

    [Header("Position on Spline")]
    public float splineProgress = 0.5f;  // Where on the spline this bucket sits (0..1)

    [Header("Visual")]
    public SpriteRenderer bucketSprite;

    // Internal
    private bool isFull;
    private ConveyorSpline conveyor;

    /// <summary>
    /// Fill ratio (0..1) for UI display.
    /// </summary>
    public float FillRatio => maxFill > 0 ? (float)currentFill / maxFill : 0f;

    /// <summary>
    /// Event raised when bucket becomes full (colorID).
    /// </summary>
    public System.Action<int> OnBucketFull;

    private void Awake()
    {
        currentFill = 0;
        isFull = false;
    }

    private void Start()
    {
        // Find the conveyor
        conveyor = FindObjectOfType<ConveyorSpline>();
        if (conveyor != null)
        {
            // Position bucket on the spline
            transform.position = conveyor.GetPositionOnSpline(splineProgress, 0f);
        }
    }

    /// <summary>
    /// Try to attract a dot. Returns true if dot was consumed.
    /// Called by FallingPixelManager each frame for nearby dots.
    /// </summary>
    public bool TryAttractDot(Dot dot)
    {
        if (isFull) return false;
        if (dot.state == Dot.DotState.InGrid) return false;
        if (dot.colorID != this.colorID) return false;

        float dist = Vector2.Distance(transform.position, dot.transform.position);
        if (dist > attractRadius) return false;

        // Pull dot toward bucket
        dot.transform.position = Vector2.MoveTowards(
            dot.transform.position, transform.position, attractSpeed * Time.deltaTime);

        // Check if dot has reached bucket center
        if (Vector2.Distance(transform.position, dot.transform.position) < 0.1f)
        {
            ConsumeDot(dot);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Consume a dot: increase fill, destroy dot, check if full.
    /// </summary>
    private void ConsumeDot(Dot dot)
    {
        currentFill++;
        Destroy(dot.gameObject);

        // Update visual scale to show fill progress
        if (bucketSprite != null)
        {
            float scale = 1f + FillRatio * 0.3f;
            bucketSprite.transform.localScale = Vector3.one * scale;
        }

        // Notify GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDotCollected(dot.colorID);
        }

        // Check if full
        if (currentFill >= maxFill)
        {
            isFull = true;
            OnBucketFull?.Invoke(colorID);

            // Animate out and destroy
            StartCoroutine(FullAnimation());
        }
    }

    private System.Collections.IEnumerator FullAnimation()
    {
        // Brief flash effect
        if (bucketSprite != null)
            bucketSprite.color = Color.white;

        yield return new WaitForSeconds(0.3f);

        // Notify game manager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBucketCompleted(this);
        }

        Destroy(gameObject);
    }
}
