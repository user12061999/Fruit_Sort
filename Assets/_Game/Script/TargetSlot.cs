using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TargetSlot : MonoBehaviour
{
    [SerializeField] private SeedColor slotColor;
    [SerializeField] private float magnetRadius = 2.2f;
    [SerializeField] private float magnetForce = 7f;
    [SerializeField] private float collectDistance = 0.16f;
    [SerializeField] private float scanInterval = 0.03f;
    [SerializeField] private ParticleSystem hitParticles;

    public SeedColor SlotColor => slotColor;

    private float nextScanTime;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Update()
    {
        if (Time.time < nextScanTime)
            return;

        nextScanTime = Time.time + scanInterval;

        IReadOnlyList<Seed> seeds = ConveyorManager.Instance.ActiveSeeds;
        float radiusSqr = magnetRadius * magnetRadius;

        for (int i = seeds.Count - 1; i >= 0; i--)
        {
            if (i >= seeds.Count)
                continue;

            Seed seed = seeds[i];
            if (seed == null || !seed.IsActiveOnBelt || seed.Color != slotColor)
                continue;

            Vector3 delta = transform.position - seed.transform.position;
            if (delta.sqrMagnitude > radiusSqr)
                continue;

            PullOrCollect(seed);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Seed seed = other.GetComponent<Seed>();
        if (seed != null && seed.Color == slotColor)
            PullOrCollect(seed);
    }

    public bool CanCollect(Seed seed)
    {
        return seed != null &&
               seed.Color == slotColor &&
               (transform.position - seed.transform.position).sqrMagnitude <= collectDistance * collectDistance;
    }

    public void CollectSeed(Seed seed)
    {
        if (!CanCollect(seed))
            return;

        if (hitParticles != null)
            hitParticles.Play();

        GameManager.Instance?.RegisterCorrectSeed(slotColor);
        seed.ReturnToPool(true);
    }

    private void PullOrCollect(Seed seed)
    {
        if (!seed.TryClaim(this))
            return;

        if (CanCollect(seed))
        {
            CollectSeed(seed);
            return;
        }

        seed.MoveByMagnet(transform.position, magnetForce);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, collectDistance);
    }
}
