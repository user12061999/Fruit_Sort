using UnityEngine;

[DisallowMultipleComponent]
public class Bucket : MonoBehaviour
{
    [Header("Identity and Fill")]
    public int colorId;
    [Min(1)] public int maxFill = 5;
    [Min(0)] public int currentFill;

    [Header("Spline Placement")]
    public ConveyorSpline conveyor;
    [Range(0f, 1f)] public float splineProgress = 0.5f;
    public float lateralOffset;
    public bool followSplinePosition = true;

    [Header("Attraction")]
    [Min(0.01f)] public float attractRadius = 0.75f;
    public bool destroyWhenFull = true;

    public float Fill01 => maxFill <= 0 ? 1f : Mathf.Clamp01((float)currentFill / maxFill);
    public bool IsFull => currentFill >= maxFill;

    private void Start()
    {
        GameManager.Instance?.RegisterBucket(this);
        UpdateSplinePosition();
    }

    private void Update()
    {
        UpdateSplinePosition();
        if (IsFull || FallingPixelManager.Instance == null)
        {
            return;
        }

        float radiusSquared = attractRadius * attractRadius;
        var dots = FallingPixelManager.Instance.activeDots;
        for (int i = 0; i < dots.Count; i++)
        {
            Dot dot = dots[i];
            if (dot == null || dot.state != DotState.OnBelt || dot.colorId != colorId || dot.IsClaimed)
            {
                continue;
            }

            if (Vector2.SqrMagnitude(dot.transform.position - transform.position) <= radiusSquared &&
                dot.TryClaim(this))
            {
                break;
            }
        }
    }

    public void AcceptDot(Dot dot)
    {
        if (dot == null || dot.targetBucket != this || IsFull)
        {
            return;
        }

        dot.targetBucket = null;
        dot.gameObject.SetActive(false);
        currentFill = Mathf.Min(maxFill, currentFill + 1);
        GameManager.Instance?.OnBucketFillChanged(this);

        if (IsFull)
        {
            GameManager.Instance?.OnBucketCompleted(this);
            if (destroyWhenFull)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void OnDisable()
    {
        if (FallingPixelManager.Instance == null)
        {
            return;
        }

        var dots = FallingPixelManager.Instance.activeDots;
        for (int i = 0; i < dots.Count; i++)
        {
            dots[i]?.ReleaseClaim(this);
        }
    }

    private void OnDestroy()
    {
        GameManager.Instance?.UnregisterBucket(this);
    }

    private void UpdateSplinePosition()
    {
        if (followSplinePosition && conveyor != null)
        {
            lateralOffset = Mathf.Clamp(lateralOffset, -conveyor.HalfWidth, conveyor.HalfWidth);
            transform.position = conveyor.GetPositionOnSpline(splineProgress, lateralOffset);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attractRadius);
    }
}
