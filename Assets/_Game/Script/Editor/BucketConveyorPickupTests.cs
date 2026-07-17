using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace FruitSort.EditorTests
{
    public sealed class BucketConveyorPickupTests
    {
        GameObject _managerObject;
        FallingPixelManager _manager;

        [SetUp]
        public void SetUp()
        {
            _managerObject = new GameObject("FallingPixelManager under test");
            _manager = _managerObject.AddComponent<FallingPixelManager>();
            _manager.beltSpeed = 2f;
            _manager.speedJitter = 0f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_managerObject);
        }

        [Test]
        public void CrossingLinkedConveyorPickup_StartsBucketAttractionOutsideBucket()
        {
            ConveyorSpline conveyor = CreateConveyor("Linked conveyor", Vector3.zero,
                new Vector3(10f, 0f), false);
            Bucket bucket = CreateBucket(new Vector3(5f, 4f));
            BindInputConveyor(bucket, conveyor);
            Dot dot = CreateBeltDot(conveyor, 0.49f);

            InvokeStepOnBelt(dot, 0.1f);

            Assert.That(dot.state, Is.EqualTo(DotState.Attracting));
            Assert.That(dot.targetBucket, Is.SameAs(bucket));
            Assert.That(bucket.ReservedCount, Is.EqualTo(1));

            Object.DestroyImmediate(dot.gameObject);
            Object.DestroyImmediate(bucket.gameObject);
            Object.DestroyImmediate(conveyor.gameObject);
        }

        [Test]
        public void BucketClick_UsesBucketVisualWithoutCollider()
        {
            ConveyorSpline conveyor = CreateConveyor("Linked conveyor", Vector3.zero,
                new Vector3(10f, 0f), false);
            Bucket bucket = CreateBucket(new Vector3(5f, 4f));
            BindInputConveyor(bucket, conveyor);
            Vector3 pickup = conveyor.GetPositionOnSpline(bucket.PickupProgress, 0f);

            MethodInfo isBucketClick = typeof(Bucket).GetMethod("IsBucketClick",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(isBucketClick, Is.Not.Null,
                "Bucket release must be triggered by clicking the bucket, not its conveyor pickup point.");
            Assert.That((bool)isBucketClick.Invoke(bucket, new object[] { bucket.MouthPosition }), Is.True);
            Assert.That((bool)isBucketClick.Invoke(bucket, new object[] { pickup }), Is.False);
            Assert.That(typeof(Bucket).GetField("zone",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), Is.Null);
            Assert.That(typeof(Bucket).GetField("attractRadius",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), Is.Null);
            Assert.That(typeof(Bucket).GetField("precisePointTest",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), Is.Null);
            Assert.That(typeof(Bucket).GetField("pickupClickRadius",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), Is.Null);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Cage.prefab");
            Assert.That(prefab.GetComponentInChildren<Collider2D>(true), Is.Null,
                "Cage prefab must not retain a click Collider2D.");

            Object.DestroyImmediate(bucket.gameObject);
            Object.DestroyImmediate(conveyor.gameObject);
        }

        [Test]
        public void DotFromAnotherConveyor_DoesNotCapture()
        {
            ConveyorSpline dotConveyor = CreateConveyor("Dot conveyor", Vector3.zero,
                new Vector3(10f, 0f), false);
            ConveyorSpline linkedConveyor = CreateConveyor("Linked conveyor", new Vector3(0f, 3f),
                new Vector3(10f, 3f), false);
            Bucket bucket = CreateBucket(new Vector3(5f, 3f));
            BindInputConveyor(bucket, linkedConveyor);
            Dot dot = CreateBeltDot(dotConveyor, 0.49f);

            InvokeStepOnBelt(dot, 0.1f);

            Assert.That(dot.state, Is.EqualTo(DotState.OnBelt));
            Assert.That(dot.targetBucket, Is.Null);
            Assert.That(bucket.ReservedCount, Is.Zero);

            Object.DestroyImmediate(dot.gameObject);
            Object.DestroyImmediate(bucket.gameObject);
            Object.DestroyImmediate(dotConveyor.gameObject);
            Object.DestroyImmediate(linkedConveyor.gameObject);
        }

        [Test]
        public void ClosedConveyor_PickupAtSeamDetectsProgressWrap()
        {
            ConveyorSpline conveyor = CreateClosedConveyor();
            Bucket bucket = CreateBucket(conveyor.GetPositionOnSpline(0f, 0f));
            BindInputConveyor(bucket, conveyor);
            Dot dot = CreateBeltDot(conveyor, 0.99f);

            InvokeStepOnBelt(dot, 0.2f);

            Assert.That(dot.state, Is.EqualTo(DotState.Attracting));
            Assert.That(dot.targetBucket, Is.SameAs(bucket));

            Object.DestroyImmediate(dot.gameObject);
            Object.DestroyImmediate(bucket.gameObject);
            Object.DestroyImmediate(conveyor.gameObject);
        }

        [Test]
        public void ReleasedDot_ClearsIgnoredBucketEvenWhileBucketIsReleasing()
        {
            ConveyorSpline conveyor = CreateClosedConveyor();
            Bucket bucket = CreateBucket(conveyor.GetPositionOnSpline(0f, 0f));
            BindInputConveyor(bucket, conveyor);
            Dot dot = CreateBeltDot(conveyor, 0.99f);
            dot.ignoredBucket = bucket;
            SetBucketReleasing(bucket, true);

            InvokeStepOnBelt(dot, 0.2f);

            Assert.That(dot.state, Is.EqualTo(DotState.OnBelt));
            Assert.That(dot.ignoredBucket, Is.Null);
            Assert.That(bucket.ReservedCount, Is.Zero);

            SetBucketReleasing(bucket, false);
            dot.beltProgress = 0.99f;
            InvokeStepOnBelt(dot, 0.2f);

            Assert.That(dot.state, Is.EqualTo(DotState.Attracting));
            Assert.That(dot.targetBucket, Is.SameAs(bucket));

            Object.DestroyImmediate(dot.gameObject);
            Object.DestroyImmediate(bucket.gameObject);
            Object.DestroyImmediate(conveyor.gameObject);
        }

        [Test]
        public void ConnectionLanding_DoesNotCaptureBucketBeforeTargetLandingProgress()
        {
            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            data.conveyorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/Conveyor.prefab").GetComponent<ConveyorSpline>();
            data.conveyors.Add(CreateConveyorData("Source", new Vector3(-4f, 0f), Vector3.zero));
            data.conveyors.Add(CreateConveyorData("Target", Vector3.zero, new Vector3(4f, 0f)));
            data.conveyorLinks.Add(new LevelData.ConveyorLink { from = 0, to = 1 });

            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorSpline source = root.transform.Find("Source").GetComponent<ConveyorSpline>();
                ConveyorSpline target = root.transform.Find("Target").GetComponent<ConveyorSpline>();
                ConveyorBeltRenderer renderer = source.GetComponent<ConveyorBeltRenderer>();
                Assert.That(renderer.TryGetConnectionRoute(target,
                    out float sourceStartProgress,
                    out float targetEndProgress,
                    out float routeLength), Is.True);
                Assert.That(targetEndProgress, Is.GreaterThan(0.01f));

                Bucket bucket = CreateBucket(target.GetPositionOnSpline(targetEndProgress * 0.5f, 0f));
                BindInputConveyor(bucket, target);
                Dot dot = CreateBeltDot(source, sourceStartProgress);
                dot.connectionTarget = target;
                dot.connectionDistance = routeLength + 0.05f;
                dot.connectionRouteLength = routeLength;
                dot.connectionTargetEndProgress = targetEndProgress;

                InvokeStepOnConnection(dot);

                Assert.That(dot.state, Is.EqualTo(DotState.OnBelt));
                Assert.That(dot.targetBucket, Is.Null,
                    "Landing at targetEndProgress must not cross the trimmed target segment before it.");
                Assert.That(bucket.ReservedCount, Is.Zero);

                Object.DestroyImmediate(dot.gameObject);
                Object.DestroyImmediate(bucket.gameObject);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void ConnectionFallback_EntersTargetAfterTrimInsteadOfKnotZero()
        {
            ConveyorSpline source = CreateConveyor("Source without renderer",
                Vector3.zero, new Vector3(10f, 0f), false);
            ConveyorSpline target = CreateConveyor("Target with renderer",
                new Vector3(10f, 0f), new Vector3(20f, 0f), false);
            source.gameObject.AddComponent<ConveyorConnections>().next.Add(target);
            target.gameObject.AddComponent<ConveyorBeltRenderer>();
            Dot dot = CreateBeltDot(source, 1f);

            try
            {
                Assert.That(InvokeAdvanceToNext(dot), Is.True);
                Assert.That(dot.conveyor, Is.SameAs(target));
                Assert.That(dot.beltProgress, Is.GreaterThan(0.01f),
                    "A fallback transfer must enter after the target's trimmed connection segment, not knot 0.");
            }
            finally
            {
                Object.DestroyImmediate(dot.gameObject);
                Object.DestroyImmediate(source.gameObject);
                Object.DestroyImmediate(target.gameObject);
            }
        }

        [Test]
        public void LevelBuilder_BindsBucketToConfiguredInputConveyor()
        {
            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            data.conveyorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/Conveyor.prefab").GetComponent<ConveyorSpline>();
            data.bucketPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/Cage.prefab").GetComponent<Bucket>();
            data.conveyors.Add(CreateConveyorData("Belt_A", Vector3.zero, new Vector3(10f, 0f)));
            data.conveyors.Add(CreateConveyorData("Belt_B", new Vector3(0f, 3f), new Vector3(10f, 3f)));

            var bucketData = new LevelData.BucketData
            {
                position = new Vector3(5f, 3f),
                colorId = 0,
                maxFill = 3,
            };
            FieldInfo inputIndex = typeof(LevelData.BucketData).GetField("inputConveyor",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(inputIndex, Is.Not.Null,
                "BucketData must serialize which conveyor feeds the bucket.");
            inputIndex.SetValue(bucketData, 1);
            data.buckets.Add(bucketData);

            GameObject root = LevelBuilder.Build(data);
            try
            {
                Bucket bucket = root.GetComponentInChildren<Bucket>();
                PropertyInfo inputProperty = typeof(Bucket).GetProperty("InputConveyor",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Assert.That(inputProperty, Is.Not.Null);
                Assert.That(inputProperty.GetValue(bucket),
                    Is.SameAs(root.transform.Find("Belt_B").GetComponent<ConveyorSpline>()));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void LevelBuilder_LegacyBucketAutoBindsNearestConveyor()
        {
            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            data.conveyorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/Conveyor.prefab").GetComponent<ConveyorSpline>();
            data.bucketPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/Cage.prefab").GetComponent<Bucket>();
            data.conveyors.Add(CreateConveyorData("Belt_A", Vector3.zero, new Vector3(10f, 0f)));
            data.conveyors.Add(CreateConveyorData("Belt_B", new Vector3(0f, 3f), new Vector3(10f, 3f)));
            data.buckets.Add(new LevelData.BucketData
            {
                position = new Vector3(5f, 3f),
                colorId = 0,
                maxFill = 3,
            });

            GameObject root = LevelBuilder.Build(data);
            try
            {
                Bucket bucket = root.GetComponentInChildren<Bucket>();
                Assert.That(bucket.InputConveyor,
                    Is.SameAs(root.transform.Find("Belt_B").GetComponent<ConveyorSpline>()));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        static ConveyorSpline CreateConveyor(string name, Vector3 start, Vector3 end, bool closed)
        {
            var go = new GameObject(name);
            SplineContainer container = go.AddComponent<SplineContainer>();
            container.Spline.Clear();
            container.Spline.Add(new BezierKnot((float3)start));
            container.Spline.Add(new BezierKnot((float3)end));
            container.Spline.Closed = closed;

            ConveyorSpline conveyor = go.AddComponent<ConveyorSpline>();
            conveyor.straightEdges = true;
            conveyor.cornerRadius = 0f;
            conveyor.bakeResolution = 64;
            conveyor.Bake();
            return conveyor;
        }

        static ConveyorSpline CreateClosedConveyor()
        {
            var go = new GameObject("Closed conveyor");
            SplineContainer container = go.AddComponent<SplineContainer>();
            container.Spline.Clear();
            container.Spline.Add(new BezierKnot(new float3(0f, 0f, 0f)));
            container.Spline.Add(new BezierKnot(new float3(4f, 0f, 0f)));
            container.Spline.Add(new BezierKnot(new float3(4f, 4f, 0f)));
            container.Spline.Add(new BezierKnot(new float3(0f, 4f, 0f)));
            container.Spline.Closed = true;

            ConveyorSpline conveyor = go.AddComponent<ConveyorSpline>();
            conveyor.straightEdges = true;
            conveyor.cornerRadius = 0f;
            conveyor.bakeResolution = 64;
            conveyor.Bake();
            return conveyor;
        }

        static LevelData.ConveyorData CreateConveyorData(string name, Vector3 start, Vector3 end)
        {
            return new LevelData.ConveyorData
            {
                name = name,
                beltWidth = 1f,
                straightEdges = true,
                cornerRadius = 0f,
                cornerSegments = 1,
                knots = new System.Collections.Generic.List<Vector3> { start, end },
            };
        }

        Bucket CreateBucket(Vector3 position)
        {
            var go = new GameObject("Bucket under test");
            go.transform.position = position;
            go.AddComponent<SpriteRenderer>();
            Bucket bucket = go.AddComponent<Bucket>();
            bucket.maxFill = 3;
            _manager.RegisterBucket(bucket);
            return bucket;
        }

        static Dot CreateBeltDot(ConveyorSpline conveyor, float progress)
        {
            var go = new GameObject("Dot under test");
            go.AddComponent<SpriteRenderer>();
            Dot dot = go.AddComponent<Dot>();
            dot.state = DotState.OnBelt;
            dot.conveyor = conveyor;
            dot.beltProgress = progress;
            dot.beltSpeedFactor = 1f;
            dot.lateralOffset = 0f;
            return dot;
        }

        static void BindInputConveyor(Bucket bucket, ConveyorSpline conveyor)
        {
            MethodInfo bind = typeof(Bucket).GetMethod("BindInputConveyor",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(bind, Is.Not.Null,
                "Bucket must expose one explicit conveyor binding instead of a spatial receiving zone.");
            bind.Invoke(bucket, new object[] { conveyor });
        }

        static void SetBucketReleasing(Bucket bucket, bool releasing)
        {
            FieldInfo field = typeof(Bucket).GetField("_releasing",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(bucket, releasing);
        }

        void InvokeStepOnBelt(Dot dot, float deltaTime)
        {
            FieldInfo cacheField = typeof(FallingPixelManager).GetField("_posCache",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Vector3[] cache = (Vector3[])cacheField.GetValue(_manager);
            cache[0] = dot.conveyor.GetPositionOnSpline(dot.beltProgress, dot.lateralOffset);

            MethodInfo step = typeof(FallingPixelManager).GetMethod("StepOnBelt",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(step, Is.Not.Null);
            step.Invoke(_manager, new object[] { dot, Vector2.zero, deltaTime, 0 });
        }

        void InvokeStepOnConnection(Dot dot)
        {
            FieldInfo cacheField = typeof(FallingPixelManager).GetField("_posCache",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Vector3[] cache = (Vector3[])cacheField.GetValue(_manager);
            cache[0] = dot.conveyor.GetPositionOnSpline(dot.beltProgress, dot.lateralOffset);

            MethodInfo step = typeof(FallingPixelManager).GetMethod("StepOnConnection",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(step, Is.Not.Null);
            bool moved = (bool)step.Invoke(_manager,
                new object[] { dot, Vector2.zero, 0f, 0, false });
            Assert.That(moved, Is.True);
        }

        bool InvokeAdvanceToNext(Dot dot)
        {
            MethodInfo advance = typeof(FallingPixelManager).GetMethod("AdvanceToNext",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(advance, Is.Not.Null);
            return (bool)advance.Invoke(_manager, new object[] { dot });
        }
    }
}
