using UnityEngine;

public enum DotState
{
    InGrid,
    Falling,
    OnBelt
}

[DisallowMultipleComponent]
public class Dot : MonoBehaviour
{
    [Header("Identity")]
    public int colorId;
    public int maxHp = 3;
    public int currentHp = 3;

    [Header("Runtime State")]
    public DotState state = DotState.InGrid;
    public Vector2Int gridPosition;
    public float splineProgress;
    public float lateralOffset;
    public Bucket targetBucket;
    public PixelGridManager gridOwner;

    public bool IsClaimed => targetBucket != null;

    public void Initialize(int newColorId, int hp, Vector2Int newGridPosition)
    {
        colorId = newColorId;
        maxHp = Mathf.Max(1, hp);
        currentHp = maxHp;
        gridPosition = newGridPosition;
        state = DotState.InGrid;
        splineProgress = 0f;
        lateralOffset = 0f;
        targetBucket = null;
        gameObject.SetActive(true);
    }

    public void TakeDamage(int damage)
    {
        if (state != DotState.InGrid || damage <= 0)
        {
            return;
        }

        currentHp = Mathf.Max(0, currentHp - damage);
        if (currentHp > 0)
        {
            return;
        }

        PixelGridManager owner = gridOwner != null
            ? gridOwner
            : GetComponentInParent<PixelGridManager>();
        if (owner != null)
        {
            owner.ReleaseDot(this);
        }
        else if (FallingPixelManager.Instance != null)
        {
            FallingPixelManager.Instance.AddDot(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool TryClaim(Bucket bucket)
    {
        if (bucket == null || targetBucket != null || state != DotState.OnBelt)
        {
            return false;
        }

        targetBucket = bucket;
        return true;
    }

    public void ReleaseClaim(Bucket bucket)
    {
        if (targetBucket == bucket)
        {
            targetBucket = null;
        }
    }
}
