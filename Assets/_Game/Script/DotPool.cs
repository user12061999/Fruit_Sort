using System.Collections.Generic;
using UnityEngine;

namespace FruitSort
{
    /// <summary>Owns reusable runtime dots created from configured prefabs.</summary>
    [DisallowMultipleComponent]
    public sealed class DotPool : MonoBehaviour
    {
        readonly Dictionary<Dot, Stack<Dot>> _availableByPrefab = new Dictionary<Dot, Stack<Dot>>();
        readonly Dictionary<Dot, Dot> _prefabByInstance = new Dictionary<Dot, Dot>();

        public Dot Acquire(Dot prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            if (!_availableByPrefab.TryGetValue(prefab, out Stack<Dot> available))
            {
                available = new Stack<Dot>();
                _availableByPrefab.Add(prefab, available);
            }

            Dot dot = available.Count > 0 ? available.Pop() : CreateInstance(prefab);
            if (dot == null) return null;

            dot.gameObject.SetActive(true);
            dot.transform.SetParent(null, false);
            dot.transform.SetPositionAndRotation(position, rotation);
            return dot;
        }

        public void Release(Dot dot)
        {
            if (dot == null || !_prefabByInstance.TryGetValue(dot, out Dot prefab)) return;

            if (dot.targetBucket != null) dot.targetBucket.CancelReservation(dot);
            dot.targetBucket = null;
            dot.ignoredBucket = null;
            dot.conveyor = null;
            dot.connectionTarget = null;
            dot.capturedByBucket = false;
            dot.capturedByGrinder = false;
            dot.markedForRemoval = false;
            dot.transform.SetParent(transform, false);
            dot.gameObject.SetActive(false);

            _availableByPrefab[prefab].Push(dot);
        }

        Dot CreateInstance(Dot prefab)
        {
            Dot instance = Instantiate(prefab, transform);
            instance.gameObject.SetActive(false);
            _prefabByInstance.Add(instance, prefab);
            return instance;
        }
    }
}
