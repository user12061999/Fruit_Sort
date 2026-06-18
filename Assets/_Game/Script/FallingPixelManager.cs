using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager for all dots that are falling or riding the conveyor belt.
/// Uses a Spatial Grid (cell-based) for efficient neighbor lookups to support 500+ dots.
/// Handles: spline movement, lateral push/separation, rotation, bucket attraction, cleanup.
/// </summary>
public class FallingPixelManager : MonoBehaviour
{
    public static FallingPixelManager Instance { get; private set; }

    [Header("References")]
    public ConveyorSpline conveyorSpline;
    public Camera mainCamera;

    [Header("Movement")]
    public float baseSpeed = 0.08f;      // Base progress per second along spline
    public float fallSpeed = 3f;         // Speed dots fall before landing on belt
    public float fallDistance = 2f;      // How far dots fall before snapping to belt
    public float speedVariation = 0.02f; // Random speed noise per dot

    [Header("Spatial Grid")]
    public float cellSize = 0.36f;       // Cell size = dotSize * 1.2
    public int maxNeighbors = 8;         // Max neighbors to process per dot
    public float pushForce = 0.5f;       // Lateral push force between close dots
    public float minSeparation = 0.25f;  // Minimum distance before push kicks in

    [Header("Rotation")]
    public float maxRotationSpeed = 60f; // Degrees per second

    // --- Internal ---
    private List<Dot> dots = new List<Dot>();
    private Dictionary<int, List<Dot>> spatialGrid = new Dictionary<int, List<Dot>>();
    private int gridWidth;               // Number of cells horizontally

    // Bounds for cleanup
    private float screenBottomY;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        UpdateScreenBounds();

        if (conveyorSpline == null)
            conveyorSpline = FindObjectOfType<ConveyorSpline>();
    }

    private void UpdateScreenBounds()
    {
        if (mainCamera != null)
        {
            screenBottomY = mainCamera.ViewportToWorldPoint(new Vector3(0, -0.1f, 0)).y;
        }
    }

    /// <summary>
    /// Register a dot into the manager (called when dot breaks from grid).
    /// </summary>
    public void AddDot(Dot dot)
    {
        dot.state = Dot.DotState.Falling;
        // Add per-dot speed variation
        dot.moveSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);
        dots.Add(dot);
    }

    /// <summary>
    /// Get current count of active dots.
    /// </summary>
    public int GetDotCount() => dots.Count;

    private void Update()
    {
        if (conveyorSpline == null) return;

        UpdateScreenBounds();
        BuildSpatialGrid();

        // Find all buckets for attraction checks
        Bucket[] buckets = FindObjectsByType<Bucket>(FindObjectsSortMode.None);

        // --- Update each dot (reverse loop for safe removal) ---
        for (int i = dots.Count - 1; i >= 0; i--)
        {
            Dot dot = dots[i];
            if (dot == null || dot.gameObject == null)
            {
                dots.RemoveAt(i);
                continue;
            }

            // --- Falling phase ---
            if (dot.state == Dot.DotState.Falling)
            {
                dot.transform.position += Vector3.down * fallSpeed * Time.deltaTime;

                // Check if dot has reached the belt (approximate Y of spline start)
                Vector3 beltStart = conveyorSpline.GetPositionOnSpline(0f, dot.lateralOffset);
                if (dot.transform.position.y <= beltStart.y)
                {
                    dot.state = Dot.DotState.OnBelt;
                    dot.splineProgress = 0f;
                    // Snap to spline start position
                    dot.transform.position = beltStart;
                }
                continue;
            }

            // --- On Belt phase ---
            if (dot.state == Dot.DotState.OnBelt)
            {
                // Advance along spline
                dot.splineProgress += dot.moveSpeed * Time.deltaTime;

                // Clamp lateral offset within belt width
                float halfWidth = conveyorSpline.beltWidth * 0.5f;
                dot.lateralOffset = Mathf.Clamp(dot.lateralOffset, -halfWidth, halfWidth);

                // Apply neighbor push (spatial grid)
                ApplyNeighborPush(dot);

                // Apply bucket attraction
                bool consumed = false;
                for (int b = 0; b < buckets.Length; b++)
                {
                    if (buckets[b] != null && buckets[b].TryAttractDot(dot))
                    {
                        consumed = true;
                        break;
                    }
                }

                if (consumed)
                {
                    dots.RemoveAt(i);
                    continue;
                }

                // Update position from spline
                Vector3 targetPos = conveyorSpline.GetPositionOnSpline(
                    dot.splineProgress, dot.lateralOffset);
                dot.transform.position = targetPos;

                // Gentle rotation
                dot.transform.Rotate(0, 0, dot.rotationSpeed * Time.deltaTime);

                // Cleanup: if dot passed end of spline or below screen
                if (dot.splineProgress >= 1f || dot.transform.position.y < screenBottomY)
                {
                    dots.RemoveAt(i);
                    Destroy(dot.gameObject);

                    // Notify GameManager of lost dot
                    if (GameManager.Instance != null)
                        GameManager.Instance.OnDotLost(dot.colorID);
                }
            }
        }
    }

    // ========== SPATIAL GRID ==========

    /// <summary>
    /// Rebuild the spatial grid from scratch each frame.
    /// Each cell is identified by a single integer hash.
    /// </summary>
    private void BuildSpatialGrid()
    {
        spatialGrid.Clear();

        // Determine grid width based on belt width
        gridWidth = Mathf.CeilToInt(conveyorSpline != null ? conveyorSpline.beltWidth / cellSize : 10);

        for (int i = 0; i < dots.Count; i++)
        {
            Dot dot = dots[i];
            if (dot == null || dot.state != Dot.DotState.OnBelt) continue;

            int cellHash = GetCellHash(dot.transform.position);

            if (!spatialGrid.ContainsKey(cellHash))
                spatialGrid[cellHash] = new List<Dot>();

            spatialGrid[cellHash].Add(dot);
        }
    }

    /// <summary>
    /// Get a unique cell hash from a world position.
    /// Uses Moore neighborhood-friendly grid coordinates.
    /// </summary>
    private int GetCellHash(Vector2 pos)
    {
        int cx = Mathf.FloorToInt(pos.x / cellSize);
        int cy = Mathf.FloorToInt(pos.y / cellSize);
        // Combine into single int (safe for typical game bounds)
        return cx * 10007 + cy;
    }

    /// <summary>
    /// Apply repulsion push from nearby dots using spatial grid lookup.
    /// Only checks Moore neighborhood (3x3 cells around the dot's cell).
    /// </summary>
    private void ApplyNeighborPush(Dot dot)
    {
        int cx = Mathf.FloorToInt(dot.transform.position.x / cellSize);
        int cy = Mathf.FloorToInt(dot.transform.position.y / cellSize);

        int neighborCount = 0;
        float totalPushX = 0f;

        // Check Moore neighborhood (3x3 grid)
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int hash = (cx + dx) * 10007 + (cy + dy);

                if (!spatialGrid.TryGetValue(hash, out List<Dot> cell))
                    continue;

                for (int j = 0; j < cell.Count && neighborCount < maxNeighbors; j++)
                {
                    Dot other = cell[j];
                    if (other == dot) continue;

                    float dist = Vector2.Distance(dot.transform.position, other.transform.position);
                    if (dist < minSeparation && dist > 0.001f)
                    {
                        // Push laterally (X direction only to keep dots on belt)
                        Vector2 dir = ((Vector2)dot.transform.position - (Vector2)other.transform.position);
                        dir.y = 0; // Only push laterally
                        totalPushX += dir.normalized.x * pushForce * (minSeparation - dist);
                        neighborCount++;
                    }
                }
            }
        }

        // Apply accumulated lateral push
        if (neighborCount > 0)
        {
            dot.lateralOffset += totalPushX * Time.deltaTime;
        }
    }

    /// <summary>
    /// Get all dots currently on the belt (for external queries).
    /// </summary>
    public List<Dot> GetActiveDots()
    {
        return new List<Dot>(dots);
    }
}
