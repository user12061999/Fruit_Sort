using System.Collections.Generic;
using UnityEngine;

public class SeedPool : MonoBehaviour
{
    public static SeedPool Instance { get; private set; }

    [SerializeField] private Seed seedPrefab;
    [SerializeField] private int poolSize = 250;
    [SerializeField] private int maxActiveSeeds = 180;
    [SerializeField] private Transform poolRoot;

    private readonly Queue<Seed> pool = new Queue<Seed>(250);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (poolRoot == null)
            poolRoot = transform;

        for (int i = 0; i < poolSize; i++)
        {
            Seed seed = Instantiate(seedPrefab, poolRoot);
            seed.SetPoolOwner(this);
            seed.ResetForPool();
            pool.Enqueue(seed);
        }
    }

    public Seed GetSeed()
    {
        if (pool.Count == 0)
            return null;
        if (ConveyorManager.Instance.ActiveSeeds.Count >= maxActiveSeeds)
            return null;

        Seed seed = pool.Dequeue();
        seed.gameObject.SetActive(true);
        ConveyorManager.Instance.RegisterSeed(seed);
        return seed;
    }

    public int ActiveCapacityLeft
    {
        get
        {
            int activeCapacity = maxActiveSeeds - ConveyorManager.Instance.ActiveSeeds.Count;
            return Mathf.Min(pool.Count, Mathf.Max(0, activeCapacity));
        }
    }

    public void ReturnSeed(Seed seed)
    {
        if (seed == null || !seed.gameObject.activeSelf)
            return;

        ConveyorManager.Instance.UnregisterSeed(seed);
        seed.ResetForPool();
        pool.Enqueue(seed);
    }
}
