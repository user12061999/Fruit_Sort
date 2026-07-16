using NUnit.Framework;
using UnityEngine;

namespace FruitSort.EditorTests
{
    public sealed class DotPoolTests
    {
        GameObject _poolObject;
        GameObject _prefabObject;
        DotPool _pool;
        Dot _prefab;

        [SetUp]
        public void SetUp()
        {
            _poolObject = new GameObject("Dot pool under test");
            _pool = _poolObject.AddComponent<DotPool>();
            _prefabObject = new GameObject("Dot prefab under test");
            _prefabObject.AddComponent<SpriteRenderer>();
            _prefab = _prefabObject.AddComponent<Dot>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_poolObject);
            Object.DestroyImmediate(_prefabObject);
        }

        [Test]
        public void ReleasedDot_IsReusedForSamePrefab()
        {
            Dot first = _pool.Acquire(_prefab, Vector3.one, Quaternion.identity);
            _pool.Release(first);

            Dot second = _pool.Acquire(_prefab, Vector3.zero, Quaternion.identity);

            Assert.That(second, Is.SameAs(first));
            Assert.That(second.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void Release_ClearsBucketReservationOwnership()
        {
            Dot dot = _pool.Acquire(_prefab, Vector3.zero, Quaternion.identity);
            GameObject bucketObject = new GameObject("Bucket under test");
            bucketObject.AddComponent<SpriteRenderer>();
            Bucket bucket = bucketObject.AddComponent<Bucket>();

            Assert.That(bucket.TryReserve(dot), Is.True);
            _pool.Release(dot);

            Assert.That(dot.targetBucket, Is.Null);
            Object.DestroyImmediate(bucketObject);
        }
    }
}
