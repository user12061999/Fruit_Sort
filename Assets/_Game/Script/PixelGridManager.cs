using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Auto-spawns falling dots from a spawn zone at the top.
/// Dots spawn in Falling state and are picked up by FallingPixelManager.
/// Configurable: dots per wave, spawn interval, colors per wave.
/// </summary>
public class PixelGridManager : MonoBehaviour
{
    [Header("Spawn Zone")]
    [Tooltip("Width of the horizontal spawn area")]
    public float spawnWidth = 5f;
    [Tooltip("Y position where dots appear (top of screen)")]
    public float spawnY = 5f;

    [Header("Wave Config")]
    [Tooltip("Number of dots spawned per wave")]
    public int dotsPerSpawn = 10;
    [Tooltip("Seconds between auto-spawn waves (0 = manual only)")]
    public float spawnInterval = 2f;
    [Tooltip("If true, auto-spawn waves on interval")]
    public bool autoSpawn = true;

    [Header("Color Config")]
    [Tooltip("How many colors to randomly pick from the palette per wave")]
    public int colorsPerSpawn = 4;
    [Tooltip("Minimum colors clamped")]
    public int minColors = 2;

    [Header("Dot Settings")]
    public float dotSize = 0.3f;
    public int defaultHP = 3;
    public Sprite dotSprite;

    [Header("Color Palette")]
    public Color[] colorPalette = new Color[]
    {
        Color.red, Color.green, Color.blue, Color.yellow, Color.magenta
    };

    [Header("Fall Behavior")]
    [Tooltip("Small random Y offset so dots don't all spawn on the same line")]
    public float randomYOffset = 0.3f;

    // Internal
    private float spawnTimer;
    private int totalDotsSpawned;
    private int nextDotID;
    private int[] batchColors;

    /// <summary>
    /// Event: total dots spawned so far.
    /// </summary>
    public System.Action<int> OnWaveSpawned;

    /// <summary>
    /// Event: dots count changed (remaining, total) for UI compatibility.
    /// </summary>
    public System.Action<int, int> OnDotsChanged;

    private void Start()
    {
        // Create default sprite if none assigned (8x8 white square)
        if (dotSprite == null)
        {
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var pixels = new Color[64];
            for (int i = 0; i < 64; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            dotSprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 32f);
        }

        spawnTimer = spawnInterval;

        // Spawn first wave immediately
        SpawnWave();
    }

    private void Update()
    {
        if (!autoSpawn || spawnInterval <= 0f) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnWave();
            spawnTimer = spawnInterval;
        }
    }

    /// <summary>
    /// Spawn a wave of falling dots.
    /// Picks random colors, creates dots in Falling state, registers them with FallingPixelManager.
    /// </summary>
    public void SpawnWave()
    {
        // Pick random colors for this wave
        int colorCount = Mathf.Clamp(colorsPerSpawn, minColors, colorPalette.Length);
        batchColors = PickRandomColors(colorCount);

        float halfWidth = spawnWidth * 0.5f;
        float centerX = transform.position.x;

        for (int i = 0; i < dotsPerSpawn; i++)
        {
            // Random X within spawn zone
            float x = centerX + Random.Range(-halfWidth, halfWidth);
            float y = spawnY + Random.Range(-randomYOffset, randomYOffset);

            // Random color from batch
            int paletteIdx = batchColors[Random.Range(0, batchColors.Length)];

            CreateFallingDot(x, y, paletteIdx);
            totalDotsSpawned++;
        }

        OnWaveSpawned?.Invoke(totalDotsSpawned);
        OnDotsChanged?.Invoke(GetRemainingDots(), totalDotsSpawned);
    }

    /// <summary>
    /// Create a single falling dot and register it with FallingPixelManager.
    /// </summary>
    private Dot CreateFallingDot(float worldX, float worldY, int colorID)
    {
        GameObject go = new GameObject($"FallDot_{nextDotID}");
        go.transform.SetParent(transform);
        go.transform.position = new Vector3(worldX, worldY, 0f);

        // SpriteRenderer
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = dotSprite;
        sr.color = colorPalette[colorID % colorPalette.Length];
        sr.sortingOrder = 10;

        // Dot component
        Dot dot = go.AddComponent<Dot>();
        dot.colorID = colorID;
        dot.maxHP = defaultHP;
        dot.currentHP = defaultHP;
        dot.dotSize = dotSize;
        dot.gridX = -1; // Not in a grid
        dot.gridY = -1;
        dot.state = Dot.DotState.InGrid; // Start as InGrid so bullets can hit
        dot.SetColor(colorPalette[colorID % colorPalette.Length]);
        dot.lateralOffset = Random.Range(-0.4f, 0.4f);

        // Collider for bullet hit detection
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(dotSize, dotSize);

        // Register with FallingPixelManager (dot will fall and then ride the belt)
        if (FallingPixelManager.Instance != null)
        {
            FallingPixelManager.Instance.AddDot(dot);
        }

        nextDotID++;
        return dot;
    }

    /// <summary>
    /// Pick N random distinct indices from the color palette.
    /// </summary>
    private int[] PickRandomColors(int count)
    {
        List<int> available = new List<int>();
        for (int i = 0; i < colorPalette.Length; i++)
            available.Add(i);

        int[] result = new int[count];
        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int idx = Random.Range(0, available.Count);
            result[i] = available[idx];
            available.RemoveAt(idx);
        }
        return result;
    }

    /// <summary>
    /// Get total dots spawned across all waves.
    /// </summary>
    public int GetTotalDotsSpawned() => totalDotsSpawned;

    /// <summary>
    /// Get remaining dots count (for UI compatibility).
    /// </summary>
    public int GetRemainingDots()
    {
        if (FallingPixelManager.Instance != null)
            return FallingPixelManager.Instance.GetDotCount();
        return 0;
    }

    /// <summary>
    /// Get total dots (for UI compatibility).
    /// </summary>
    public int GetTotalDots() => totalDotsSpawned;

    /// <summary>
    /// Called when a dot is broken by a bullet (for GameManager notification).
    /// </summary>
    public void OnDotBroken(Dot dot)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnDotCollected(dot.colorID);
    }

    /// <summary>
    /// Clear all spawned dots.
    /// </summary>
    public void ClearAllDots()
    {
        // Destroy all child dots
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// Reset spawner (called on level change).
    /// </summary>
    public void SpawnBatch()
    {
        ClearAllDots();
        totalDotsSpawned = 0;
        nextDotID = 0;
        SpawnWave();
    }

    /// <summary>
    /// Draw spawn zone gizmo in the editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
        Vector3 center = new Vector3(transform.position.x, spawnY, 0);
        Gizmos.DrawWireCube(center, new Vector3(spawnWidth, randomYOffset * 2f, 0));
    }
}
