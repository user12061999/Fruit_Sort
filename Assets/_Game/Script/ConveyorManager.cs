using System.Collections.Generic;
using UnityEngine;

public class ConveyorManager : MonoBehaviour
{
    public static ConveyorManager Instance { get; private set; }

    [SerializeField] private float baseSpeed = 2.5f;

    private readonly List<Seed> activeSeeds = new List<Seed>(250);

    public IReadOnlyList<Seed> ActiveSeeds => activeSeeds;
    public float CurrentSpeed { get; private set; }
    public float BaseSpeed => baseSpeed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CurrentSpeed = baseSpeed;
    }

    public void RegisterSeed(Seed seed)
    {
        if (seed != null && !activeSeeds.Contains(seed))
            activeSeeds.Add(seed);
    }

    public void UnregisterSeed(Seed seed)
    {
        if (seed != null)
            activeSeeds.Remove(seed);
    }

    public void SetSpeed(float multiplier)
    {
        CurrentSpeed = baseSpeed * Mathf.Max(0f, multiplier);
    }

    public void SetRawSpeed(float speed)
    {
        CurrentSpeed = Mathf.Max(0f, speed);
    }
}
