using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public class FallingPixelManager : MonoBehaviour
{
    public static FallingPixelManager Instance { get; private set; }

    [Header("References")]
    public ConveyorSpline conveyor;
    public List<Dot> activeDots = new List<Dot>(500);

    [Header("Falling")]
    [Min(0.01f)] public float fallSpeed = 4f;
    [Min(0.001f)] public float beltSnapDistance = 0.05f;
    public float despawnY = -20f;

    [Header("Belt Motion")]
    [Min(0.01f)] public float beltSpeed = 1.5f;
    [Range(0f, 1f)] public float speedNoise = 0.12f;
    [Min(0f)] public float rotationSpeed = 35f;

    [Header("Spatial Grid Separation")]
    [Min(0.01f)] public float dotSize = 0.25f;
    [Min(1f)] public float cellSizeMultiplier = 1.2f;
    [Min(0f)] public float separationStrength = 4f;
    [Range(1, 64)] public int maxNeighborsPerDot = 12;
    [Min(1)] public int maxDots = 500;

    [Header("Bucket Attraction")]
    [Min(0.01f)] public float bucketAttractSpeed = 8f;
    [Min(0.001f)] public float bucketConsumeDistance = 0.08f;

    private readonly Dictionary<Vector2Int, List<int>> spatialGrid =
        new Dictionary<Vector2Int, List<int>>(512);
    private Vector2[] separationBuffer = new Vector2[500];

    public float CellSize => Mathf.Max(0.01f, dotSize * cellSizeMultiplier);
    public int ActiveDotCount => activeDots.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AddDot(Dot dot)
    {
        if (dot == null || activeDots.Contains(dot))
        {
            return;
        }

        if (activeDots.Count >= maxDots)
        {
            Debug.LogWarning("FallingPixelManager reached maxDots; discarding the new dot.", this);
            Destroy(dot.gameObject);
            return;
        }

        dot.state = DotState.Falling;
        dot.splineProgress = 0f;
        dot.targetBucket = null;
        dot.lateralOffset = conveyor != null
            ? Random.Range(-conveyor.HalfWidth, conveyor.HalfWidth)
            : 0f;
        activeDots.Add(dot);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        UpdateMotion(deltaTime);
        SeparateDots(deltaTime);
        RemoveFinishedDots();
    }

    private void UpdateMotion(float deltaTime)
    {
        float splineLength = conveyor != null ? Mathf.Max(0.001f, conveyor.GetSplineLength()) : 1f;
        for (int i = 0; i < activeDots.Count; i++)
        {
            Dot dot = activeDots[i];
            if (dot == null)
            {
                continue;
            }

            if (dot.targetBucket != null)
            {
                UpdateBucketAttraction(dot, deltaTime);
                continue;
            }

            if (dot.state == DotState.Falling)
            {
                Vector3 target = conveyor != null
                    ? conveyor.GetPositionOnSpline(0f, dot.lateralOffset)
                    : new Vector3(dot.transform.position.x, despawnY, dot.transform.position.z);
                dot.transform.position = Vector3.MoveTowards(dot.transform.position, target,
                    fallSpeed * deltaTime);

                if (conveyor != null && Vector2.SqrMagnitude(dot.transform.position - target) <=
                    beltSnapDistance * beltSnapDistance)
                {
                    dot.state = DotState.OnBelt;
                    dot.transform.position = target;
                }
            }
            else if (dot.state == DotState.OnBelt && conveyor != null)
            {
                float noise = 1f + Mathf.Sin(dot.GetInstanceID() * 0.017f + Time.time * 1.7f) * speedNoise;
                dot.splineProgress += beltSpeed * noise * deltaTime / splineLength;
                dot.lateralOffset = Mathf.Clamp(dot.lateralOffset, -conveyor.HalfWidth, conveyor.HalfWidth);
                dot.transform.position = conveyor.GetPositionOnSpline(dot.splineProgress, dot.lateralOffset);
            }

            float rotationDirection = (dot.GetInstanceID() & 1) == 0 ? 1f : -1f;
            dot.transform.Rotate(0f, 0f, rotationSpeed * rotationDirection * deltaTime);
        }
    }

    private void UpdateBucketAttraction(Dot dot, float deltaTime)
    {
        Bucket bucket = dot.targetBucket;
        if (bucket == null || !bucket.isActiveAndEnabled)
        {
            dot.targetBucket = null;
            return;
        }

        dot.transform.position = Vector3.MoveTowards(dot.transform.position, bucket.transform.position,
            bucketAttractSpeed * deltaTime);
        if (Vector2.SqrMagnitude(dot.transform.position - bucket.transform.position) <=
            bucketConsumeDistance * bucketConsumeDistance)
        {
            bucket.AcceptDot(dot);
        }
    }

    private void SeparateDots(float deltaTime)
    {
        EnsureSeparationCapacity(activeDots.Count);
        spatialGrid.Clear();
        for (int i = 0; i < activeDots.Count; i++)
        {
            separationBuffer[i] = Vector2.zero;
            Dot dot = activeDots[i];
            if (dot == null || dot.IsClaimed)
            {
                continue;
            }

            Vector2Int cell = PositionToCell(dot.transform.position);
            if (!spatialGrid.TryGetValue(cell, out List<int> indices))
            {
                indices = new List<int>(8);
                spatialGrid.Add(cell, indices);
            }

            indices.Add(i);
        }

        float minimumDistance = dotSize;
        float minimumDistanceSquared = minimumDistance * minimumDistance;
        for (int i = 0; i < activeDots.Count; i++)
        {
            Dot dot = activeDots[i];
            if (dot == null || dot.IsClaimed)
            {
                continue;
            }

            int neighborsChecked = 0;
            Vector2Int centerCell = PositionToCell(dot.transform.position);
            for (int y = -1; y <= 1 && neighborsChecked < maxNeighborsPerDot; y++)
            {
                for (int x = -1; x <= 1 && neighborsChecked < maxNeighborsPerDot; x++)
                {
                    if (!spatialGrid.TryGetValue(centerCell + new Vector2Int(x, y),
                        out List<int> indices))
                    {
                        continue;
                    }

                    for (int n = 0; n < indices.Count && neighborsChecked < maxNeighborsPerDot; n++)
                    {
                        int otherIndex = indices[n];
                        if (otherIndex <= i)
                        {
                            continue;
                        }

                        Dot other = activeDots[otherIndex];
                        if (other == null || other.IsClaimed)
                        {
                            continue;
                        }

                        neighborsChecked++;
                        Vector2 delta = (Vector2)dot.transform.position -
                            (Vector2)other.transform.position;
                        float distanceSquared = delta.sqrMagnitude;
                        if (distanceSquared >= minimumDistanceSquared)
                        {
                            continue;
                        }

                        float distance = Mathf.Sqrt(Mathf.Max(distanceSquared, 0.000001f));
                        Vector2 direction = distanceSquared < 0.000001f
                            ? Random.insideUnitCircle.normalized
                            : delta / distance;
                        Vector2 push = direction * ((minimumDistance - distance) * 0.5f);
                        separationBuffer[i] += push;
                        separationBuffer[otherIndex] -= push;
                    }
                }
            }
        }

        float blend = Mathf.Clamp01(separationStrength * deltaTime);
        for (int i = 0; i < activeDots.Count; i++)
        {
            Dot dot = activeDots[i];
            Vector2 push = separationBuffer[i] * blend;
            if (dot == null || dot.IsClaimed || push.sqrMagnitude <= 0f)
            {
                continue;
            }

            if (dot.state == DotState.OnBelt && conveyor != null)
            {
                Vector2 normal = conveyor.GetNormalOnSpline(dot.splineProgress);
                dot.lateralOffset = Mathf.Clamp(dot.lateralOffset + Vector2.Dot(push, normal),
                    -conveyor.HalfWidth, conveyor.HalfWidth);
                dot.transform.position = conveyor.GetPositionOnSpline(dot.splineProgress,
                    dot.lateralOffset);
            }
            else
            {
                dot.transform.position += (Vector3)push;
            }
        }
    }

    private void RemoveFinishedDots()
    {
        // Reverse iteration keeps removal safe and avoids skipped entries.
        for (int i = activeDots.Count - 1; i >= 0; i--)
        {
            Dot dot = activeDots[i];
            bool consumed = dot != null && !dot.gameObject.activeSelf;
            bool reachedEnd = dot != null && dot.state == DotState.OnBelt && dot.splineProgress >= 1f;
            bool fellOffScreen = dot != null && dot.state == DotState.Falling &&
                dot.transform.position.y < despawnY;
            if (dot == null || consumed || reachedEnd || fellOffScreen)
            {
                activeDots.RemoveAt(i);
                if (dot != null)
                {
                    Destroy(dot.gameObject);
                }
            }
        }
    }

    private Vector2Int PositionToCell(Vector3 position)
    {
        float inverseCellSize = 1f / CellSize;
        return new Vector2Int(Mathf.FloorToInt(position.x * inverseCellSize),
            Mathf.FloorToInt(position.y * inverseCellSize));
    }

    private void EnsureSeparationCapacity(int count)
    {
        if (separationBuffer.Length < count)
        {
            separationBuffer = new Vector2[Mathf.NextPowerOfTwo(count)];
        }
    }
}
