using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Seed : MonoBehaviour
{
    private enum SeedState
    {
        Idle,
        FlyingToBelt,
        OnBelt,
        Magnetized
    }

    [SerializeField] private float flySpeed = 7f;
    [SerializeField] private float despawnX = 11f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Renderer visualRenderer;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private bool enableTrail = false;
    [SerializeField] private int maxTrailActiveSeeds = 45;

    private Rigidbody2D rb;
    private Collider2D triggerCollider;
    private SeedPool poolOwner;
    private SeedState state;
    private Vector3 beltPoint;
    private TargetSlot claimedSlot;
    private MaterialPropertyBlock propertyBlock;

    public SeedColor Color { get; private set; }
    public bool IsActiveOnBelt => state == SeedState.OnBelt || state == SeedState.Magnetized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        triggerCollider = GetComponent<Collider2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        triggerCollider.isTrigger = true;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (visualRenderer == null)
            visualRenderer = GetComponentInChildren<Renderer>();
        if (trailRenderer == null)
            trailRenderer = GetComponentInChildren<TrailRenderer>();
        if (trailRenderer != null)
            trailRenderer.enabled = enableTrail;

        propertyBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (state == SeedState.FlyingToBelt)
        {
            transform.position = Vector3.MoveTowards(transform.position, beltPoint, flySpeed * Time.deltaTime);
            if ((transform.position - beltPoint).sqrMagnitude <= 0.0025f)
                state = SeedState.OnBelt;
            return;
        }

        if (state == SeedState.OnBelt)
        {
            transform.Translate(Vector3.right * ConveyorManager.Instance.CurrentSpeed * Time.deltaTime, Space.World);

            if (transform.position.x > despawnX)
                ReturnToPool(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TargetSlot slot = other.GetComponent<TargetSlot>();
        if (slot != null && slot.CanCollect(this))
            slot.CollectSeed(this);
    }

    public void SetPoolOwner(SeedPool owner)
    {
        poolOwner = owner;
    }

    public void FlyToBelt(Vector3 startPosition, Vector3 targetBeltPoint, SeedColor seedColor)
    {
        transform.position = startPosition;
        beltPoint = targetBeltPoint;
        claimedSlot = null;
        state = SeedState.FlyingToBelt;
        SetColor(seedColor);

        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            bool shouldUseTrail = enableTrail && ConveyorManager.Instance.ActiveSeeds.Count <= maxTrailActiveSeeds;
            trailRenderer.enabled = shouldUseTrail;
            trailRenderer.emitting = shouldUseTrail;
        }
    }

    public bool TryClaim(TargetSlot slot)
    {
        if (claimedSlot != null && claimedSlot != slot)
            return false;

        claimedSlot = slot;
        state = SeedState.Magnetized;
        return true;
    }

    public void MoveByMagnet(Vector3 targetPosition, float force)
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, force * Time.deltaTime);
    }

    public void ReturnToPool(bool collected)
    {
        if (state == SeedState.Idle)
            return;

        if (!collected)
            GameManager.Instance?.RegisterMissedSeed(this);

        poolOwner.ReturnSeed(this);
    }

    public void ResetForPool()
    {
        state = SeedState.Idle;
        claimedSlot = null;

        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
            trailRenderer.enabled = enableTrail;
            trailRenderer.Clear();
        }

        gameObject.SetActive(false);
    }

    private void SetColor(SeedColor seedColor)
    {
        Color = seedColor;
        UnityEngine.Color unityColor = ToUnityColor(seedColor);

        if (spriteRenderer != null)
            spriteRenderer.color = unityColor;

        if (visualRenderer != null)
        {
            visualRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", unityColor);
            propertyBlock.SetColor("_BaseColor", unityColor);
            visualRenderer.SetPropertyBlock(propertyBlock);
        }

        if (trailRenderer != null)
        {
            trailRenderer.startColor = unityColor;
            trailRenderer.endColor = new UnityEngine.Color(unityColor.r, unityColor.g, unityColor.b, 0f);
        }
    }

    private static UnityEngine.Color ToUnityColor(SeedColor seedColor)
    {
        switch (seedColor)
        {
            case SeedColor.Red:
                return new UnityEngine.Color(1f, 0.22f, 0.18f);
            case SeedColor.Blue:
                return new UnityEngine.Color(0.18f, 0.45f, 1f);
            case SeedColor.Yellow:
                return new UnityEngine.Color(1f, 0.86f, 0.18f);
            case SeedColor.Green:
                return new UnityEngine.Color(0.2f, 0.85f, 0.35f);
            default:
                return UnityEngine.Color.white;
        }
    }
}
