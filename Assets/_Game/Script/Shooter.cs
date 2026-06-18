using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player shooter: fires bullets straight up from a fixed position.
/// Uses Physics2D OverlapPoint to detect dot hits.
/// No physics collision between dots — only bullet-dot interaction.
/// Compatible with Unity's new Input System package.
/// </summary>
public class Shooter : MonoBehaviour
{
    [Header("Shooting Settings")]
    public float fireRate = 0.25f;      // Seconds between shots
    public float bulletSpeed = 12f;     // Bullet travel speed
    public float bulletLifetime = 2f;   // Seconds before bullet is destroyed
    public int bulletDamage = 1;        // Damage per hit

    [Header("Position")]
    public float shootY = -4f;          // Y position of the shooter
    public float minX = -4f;            // Left bound for shooter movement
    public float maxX = 4f;             // Right bound for shooter movement
    public float moveSpeed = 6f;        // Horizontal movement speed

    [Header("Prefabs")]
    public GameObject bulletPrefab;     // Bullet prefab (optional; created if null)
    public Sprite bulletSprite;

    // Internal
    private float fireCooldown;
    private Camera mainCamera;

    // New Input System
    private InputAction moveAction;
    private InputAction shootAction;

    private void Awake()
    {
        // Setup Input Actions for new Input System
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
        // Also support arrow keys
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/rightArrow");

        shootAction = new InputAction("Shoot", InputActionType.Button);
        shootAction.AddBinding("<Keyboard>/space");

        moveAction.Enable();
        shootAction.Enable();
    }

    private void OnDestroy()
    {
        moveAction?.Dispose();
        shootAction?.Dispose();
    }

    private void Start()
    {
        mainCamera = Camera.main;
        transform.position = new Vector3(0, shootY, 0);

        // Create default bullet prefab if none assigned
        if (bulletPrefab == null)
        {
            CreateDefaultBulletPrefab();
        }
    }

    private void Update()
    {
        // --- Movement (New Input System) ---
        float hInput = moveAction.ReadValue<float>();

        float newX = transform.position.x + hInput * moveSpeed * Time.deltaTime;
        newX = Mathf.Clamp(newX, minX, maxX);
        transform.position = new Vector3(newX, shootY, 0f);

        // --- Shooting ---
        fireCooldown -= Time.deltaTime;

        if (shootAction.ReadValue<float>() > 0f && fireCooldown <= 0f)
        {
            Fire();
            fireCooldown = fireRate;
        }
    }

    /// <summary>
    /// Fire a bullet straight up from current position.
    /// </summary>
    private void Fire()
    {
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        bullet.name = "Bullet";
        bullet.SetActive(true);  // Prefab is inactive, must activate the clone

        // Setup bullet behavior
        BulletBehavior bb = bullet.GetComponent<BulletBehavior>();
        if (bb == null)
            bb = bullet.AddComponent<BulletBehavior>();

        bb.Initialize(Vector2.up * bulletSpeed, bulletLifetime, bulletDamage);
    }

    /// <summary>
    /// Create a simple default bullet prefab at runtime.
    /// </summary>
    private void CreateDefaultBulletPrefab()
    {
        bulletPrefab = new GameObject("BulletPrefab");
        bulletPrefab.hideFlags = HideFlags.HideInHierarchy;

        SpriteRenderer sr = bulletPrefab.AddComponent<SpriteRenderer>();
        // Create default bullet sprite if none assigned
        if (bulletSprite == null)
        {
            var tex = new Texture2D(4, 8, TextureFormat.RGBA32, false);
            var px = new Color[32];
            for (int i = 0; i < 32; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            bulletSprite = Sprite.Create(tex, new Rect(0, 0, 4, 8), new Vector2(0.5f, 0.25f), 32f);
        }
        sr.sprite = bulletSprite;
        sr.color = Color.white;
        sr.sortingOrder = 20;
        sr.transform.localScale = Vector3.one * 0.5f;

        BoxCollider2D col = bulletPrefab.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.1f, 0.2f);

        bulletPrefab.SetActive(false);
    }
}

/// <summary>
/// Simple bullet behavior: moves in a direction and checks for dot hits.
/// Attached to each bullet at spawn time.
/// </summary>
public class BulletBehavior : MonoBehaviour
{
    private Vector2 velocity;
    private float lifetime;
    private int damage;
    private float timer;

    public void Initialize(Vector2 vel, float life, int dmg)
    {
        velocity = vel;
        lifetime = life;
        damage = dmg;
        timer = 0f;
    }

    private void Update()
    {
        // Move via Transform (no Rigidbody)
        transform.position += (Vector3)(velocity * Time.deltaTime);
        timer += Time.deltaTime;

        // Check for dot hits using OverlapPoint
        Collider2D hit = Physics2D.OverlapPoint(transform.position);
        if (hit != null)
        {
            Dot dot = hit.GetComponent<Dot>();
            if (dot != null && (dot.state == Dot.DotState.InGrid || dot.state == Dot.DotState.Falling))
            {
                dot.TakeDamage(damage);

                // Notify PixelGridManager for tracking
                if (dot.currentHP <= 0)
                {
                    PixelGridManager gridMgr = FindObjectOfType<PixelGridManager>();
                    if (gridMgr != null)
                        gridMgr.OnDotBroken(dot);
                }

                Destroy(gameObject);
                return;
            }
        }

        // Destroy after lifetime or if off-screen
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
