using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SeedBox : MonoBehaviour
{
    [SerializeField] private SeedColor boxColor;
    [SerializeField] private Transform beltDropPoint;
    [SerializeField] private Vector2 randomDropOffset = new Vector2(0.65f, 0.2f);
    [SerializeField] private int minSeedsPerBurst = 5;
    [SerializeField] private int maxSeedsPerBurst = 10;
    [SerializeField] private float burstInterval = 0.1f;

    private Coroutine spawnRoutine;

    private void OnMouseDown()
    {
        if (spawnRoutine == null)
            spawnRoutine = StartCoroutine(SpawnBurst());
    }

    private void OnMouseUp()
    {
        StopSpawning();
    }

    private void OnDisable()
    {
        StopSpawning();
    }

    private IEnumerator SpawnBurst()
    {
        WaitForSeconds wait = new WaitForSeconds(burstInterval);

        while (true)
        {
            int capacityLeft = SeedPool.Instance.ActiveCapacityLeft;
            int count = Mathf.Min(Random.Range(minSeedsPerBurst, maxSeedsPerBurst + 1), capacityLeft);
            for (int i = 0; i < count; i++)
            {
                Seed seed = SeedPool.Instance.GetSeed();
                if (seed == null)
                    break;

                Vector3 target = GetDropPoint();
                seed.FlyToBelt(transform.position, target, boxColor);
            }

            yield return wait;
        }
    }

    private Vector3 GetDropPoint()
    {
        Vector3 center = beltDropPoint != null ? beltDropPoint.position : transform.position + Vector3.down * 3f;
        center.x += Random.Range(-randomDropOffset.x, randomDropOffset.x);
        center.y += Random.Range(-randomDropOffset.y, randomDropOffset.y);
        return center;
    }

    private void StopSpawning()
    {
        if (spawnRoutine == null)
            return;

        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }
}
