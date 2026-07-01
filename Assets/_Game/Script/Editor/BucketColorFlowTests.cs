using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FruitSort.EditorTests
{
    public sealed class BucketColorFlowTests
    {
        GameObject _bucketObject;
        Bucket _bucket;

        [SetUp]
        public void SetUp()
        {
            _bucketObject = new GameObject("Bucket under test");
            _bucket = _bucketObject.AddComponent<Bucket>();
            _bucket.maxFill = 3;
            _bucket.colorId = 1;
            _bucket.color = Color.green;
            _bucket.wrongColorLerpDuration = 0f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_bucketObject);
        }

        [Test]
        public void EmptyBucket_AcceptsAnyColor()
        {
            Assert.That(_bucket.CanAcceptColor(0), Is.True);
            Assert.That(_bucket.CanAcceptColor(1), Is.True);
            Assert.That(_bucket.CanAcceptColor(2), Is.True);
        }

        [Test]
        public void FirstReservedDot_LocksBucketToThatColor()
        {
            Dot dot = CreateDot(2, Color.blue);

            Assert.That(_bucket.TryReserve(dot), Is.True);
            Assert.That(_bucket.ContainedColorId, Is.EqualTo(2));
            Assert.That(_bucket.CanAcceptColor(2), Is.True);
            Assert.That(_bucket.CanAcceptColor(1), Is.False);

            Object.DestroyImmediate(dot.gameObject);
        }

        [Test]
        public void WrongColor_TintsBodyButCorrectColorKeepsWhite()
        {
            Dot wrongDot = CreateDot(2, Color.blue);
            Assert.That(_bucket.TryReserve(wrongDot), Is.True);
            Assert.That(_bucket.body.color, Is.EqualTo(Color.blue));

            _bucket.CancelReservation(wrongDot);
            Assert.That(_bucket.body.color, Is.EqualTo(Color.white));

            Dot correctDot = CreateDot(1, Color.green);
            Assert.That(_bucket.TryReserve(correctDot), Is.True);
            Assert.That(_bucket.body.color, Is.EqualTo(Color.white));

            Object.DestroyImmediate(wrongDot.gameObject);
            Object.DestroyImmediate(correctDot.gameObject);
        }

        [Test]
        public void CancellingOnlyReservation_UnlocksBucketForAnotherColor()
        {
            Dot dot = CreateDot(2, Color.blue);
            Assert.That(_bucket.TryReserve(dot), Is.True);

            _bucket.CancelReservation(dot);

            Assert.That(_bucket.ContainedColorId, Is.EqualTo(-1));
            Assert.That(_bucket.CanAcceptColor(0), Is.True);

            Object.DestroyImmediate(dot.gameObject);
        }

        [Test]
        public void ReleasedReservation_IgnoresTheBucketThatReleasedIt()
        {
            Dot dot = CreateDot(2, Color.blue);
            Assert.That(_bucket.TryReserve(dot), Is.True);

            Assert.That(_bucket.ReleaseContents(), Is.True);

            Assert.That(dot.ignoredBucket, Is.SameAs(_bucket));
            Assert.That(dot.state, Is.EqualTo(DotState.OnBelt));
            Object.DestroyImmediate(dot.gameObject);
        }

        [Test]
        public void DotWithDedicatedSprite_IsStillTintedByItsColorIdColor()
        {
            Dot dot = CreateDot(7, Color.magenta);
            var texture = new Texture2D(1, 1);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);

            dot.Init(7, Color.magenta, 1, Vector2Int.zero, sprite);

            Assert.That(dot.Sr.color, Is.EqualTo(Color.magenta));
            Object.DestroyImmediate(dot.gameObject);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void FixedColorId_UsesFruitDatabaseInsteadOfFourColorPaletteClamp()
        {
            var spawnerObject = new GameObject("Spawner under test");
            ModelDotSpawner spawner = spawnerObject.AddComponent<ModelDotSpawner>();
            FruitDatabase database = ScriptableObject.CreateInstance<FruitDatabase>();
            FruitData fruit = ScriptableObject.CreateInstance<FruitData>();
            fruit.colorId = 9;
            fruit.color = Color.magenta;
            database.fruits = new[] { fruit };
            spawner.fruitDatabase = database;
            spawner.fixedColorId = 9;
            spawner.palette = new[] { Color.red, Color.green, Color.blue, Color.yellow };

            Assert.That(spawner.TryResolveSpawnAppearance(out int colorId, out Color color, out _), Is.True);
            Assert.That(colorId, Is.EqualTo(9));
            Assert.That(color, Is.EqualTo(Color.magenta));

            Object.DestroyImmediate(spawnerObject);
            Object.DestroyImmediate(database);
            Object.DestroyImmediate(fruit);
        }

        [Test]
        public void RefreshVisuals_UsesSeparateBackgroundAndFillLayers()
        {
            var backgroundObject = new GameObject("Fruit Background");
            backgroundObject.transform.SetParent(_bucketObject.transform);
            _bucket.background = backgroundObject.AddComponent<SpriteRenderer>();
            _bucket.backgroundColor = new Color(1f, 1f, 1f, 0.2f);

            var texture = new Texture2D(1, 1);
            Sprite fruitSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            FruitDatabase database = ScriptableObject.CreateInstance<FruitDatabase>();
            FruitData fruit = ScriptableObject.CreateInstance<FruitData>();
            fruit.colorId = _bucket.colorId;
            fruit.color = Color.green;
            fruit.sprite = fruitSprite;
            database.fruits = new[] { fruit };
            _bucket.fruitDatabase = database;

            _bucket.RefreshVisuals();

            Assert.That(_bucket.body.sprite, Is.SameAs(fruitSprite));
            Assert.That(_bucket.background.sprite, Is.SameAs(fruitSprite));
            Assert.That(_bucket.body.color, Is.EqualTo(Color.white));
            Assert.That(_bucket.background.color, Is.EqualTo(_bucket.backgroundColor));

            Object.DestroyImmediate(database);
            Object.DestroyImmediate(fruit);
            Object.DestroyImmediate(fruitSprite);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CagePrefab_HasConfiguredBackgroundAndFillLayers()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Cage.prefab");
            Bucket bucket = prefab.GetComponent<Bucket>();

            Assert.That(bucket, Is.Not.Null);
            Assert.That(bucket.fruitDatabase, Is.Not.Null);
            Assert.That(bucket.body, Is.Not.Null);
            Assert.That(bucket.background, Is.Not.Null);
            Assert.That(bucket.background, Is.Not.SameAs(bucket.body));
            Assert.That(bucket.body.enabled, Is.True);
            Assert.That(bucket.background.enabled, Is.True);
            Assert.That(bucket.background.sortingOrder, Is.LessThan(bucket.body.sortingOrder));
            Assert.That(bucket.gridFill.GetComponent<SpriteRenderer>(), Is.SameAs(bucket.body));
        }

        Dot CreateDot(int colorId, Color color)
        {
            var dotObject = new GameObject($"Dot {colorId}");
            dotObject.AddComponent<SpriteRenderer>();
            Dot dot = dotObject.AddComponent<Dot>();
            dot.colorId = colorId;
            dot.color = color;
            return dot;
        }
    }
}
