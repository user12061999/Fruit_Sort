using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace FruitSort.EditorTests
{
    public sealed class GamePlayMoveLimitTests
    {
        GameObject _managerObject;
        GamePlayManager _manager;
        GamePlayManager _previousInstance;

        [SetUp]
        public void SetUp()
        {
            _previousInstance = GamePlayManager.Instance;
            SetGamePlayManagerInstance(null);

            _managerObject = new GameObject("GamePlayManager under test");
            _manager = _managerObject.AddComponent<GamePlayManager>();
            _manager.moveLimit = 1;
            _manager.ResetMoves();
            SetGamePlayManagerInstance(_manager);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_managerObject);
            SetGamePlayManagerInstance(_previousInstance);
        }

        [Test]
        public void LastMoveWithoutCompletableBucket_LosesAndBlocksFurtherMoves()
        {
            Assert.That(_manager.CanUseMove, Is.True);

            Assert.That(_manager.RecordInteraction(), Is.True);

            Assert.That(_manager.MovesLeft, Is.EqualTo(0));
            Assert.That(_manager.HasLost, Is.True);
            Assert.That(_manager.CanUseMove, Is.False);
        }

        [Test]
        public void ConfigureFromLevel_AppliesMoveAndTimeLimits()
        {
            LevelData level = ScriptableObject.CreateInstance<LevelData>();
            level.moveLimit = 7;
            level.timeLimit = 45f;

            _manager.ConfigureFromLevel(level);

            Assert.That(_manager.moveLimit, Is.EqualTo(7));
            Assert.That(_manager.MovesLeft, Is.EqualTo(7));
            Assert.That(_manager.movesLeftDebug, Is.EqualTo(7));
            Assert.That(_manager.timeLimit, Is.EqualTo(45f));
            Assert.That(_manager.TimeLeft, Is.EqualTo(45f).Within(0.001f));
            Assert.That(_manager.timeLeftDebug, Is.EqualTo(45f).Within(0.001f));
            Assert.That(_manager.IsTimeCounting, Is.False);
            Assert.That(_manager.HasStartedInteraction, Is.False);

            Object.DestroyImmediate(level);
        }

        [Test]
        public void FirstInteraction_StartsTimeCounting()
        {
            _manager.moveLimit = 3;
            _manager.timeLimit = 20f;
            _manager.ResetMoves();
            _manager.ResetTime();

            Assert.That(_manager.IsTimeCounting, Is.False);
            Assert.That(_manager.HasStartedInteraction, Is.False);

            Assert.That(_manager.RecordInteraction(), Is.True);

            Assert.That(_manager.IsTimeCounting, Is.True);
            Assert.That(_manager.HasStartedInteraction, Is.True);
            Assert.That(_manager.MovesLeft, Is.EqualTo(2));
        }

        [Test]
        public void LastMoveWithoutCompletableBucket_InvokesLoseEvent()
        {
            int loseCount = 0;
            _manager.onLose.AddListener(() => loseCount++);

            Assert.That(_manager.RecordInteraction(), Is.True);

            Assert.That(loseCount, Is.EqualTo(1));
        }

        [Test]
        public void LastMoveWithEnoughMatchingActiveDots_DefersLoseUntilBucketCanWin()
        {
            GameObject fallingObject = new GameObject("FallingPixelManager under test");
            FallingPixelManager fallingManager = fallingObject.AddComponent<FallingPixelManager>();
            _manager.fallingManager = fallingManager;

            GameObject bucketObject = new GameObject("Almost full bucket");
            bucketObject.AddComponent<SpriteRenderer>();
            Bucket bucket = bucketObject.AddComponent<Bucket>();
            bucket.colorId = 2;
            bucket.maxFill = 3;
            bucket.currentFill = 2;
            bucket.RefreshVisuals();

            Dot dot = CreateDot(2, Color.blue);
            fallingManager.LaunchDot(dot, Vector3.zero, Vector2.down, 1f, 0f);

            Assert.That(_manager.RecordInteraction(), Is.True);

            Assert.That(_manager.MovesLeft, Is.EqualTo(0));
            Assert.That(_manager.HasLost, Is.False);
            Assert.That(_manager.CanUseMove, Is.False);

            bucket.AddFill(1);

            Assert.That(_manager.HasWon, Is.True);
            Object.DestroyImmediate(bucketObject);
            Object.DestroyImmediate(fallingObject);
        }

        [Test]
        public void LastMoveWithEnoughPendingReleaseDots_DefersLoseUntilBucketCanWin()
        {
            GameObject fallingObject = new GameObject("FallingPixelManager under test");
            FallingPixelManager fallingManager = fallingObject.AddComponent<FallingPixelManager>();
            _manager.fallingManager = fallingManager;

            GameObject targetObject = new GameObject("Almost full target bucket");
            targetObject.AddComponent<SpriteRenderer>();
            Bucket target = targetObject.AddComponent<Bucket>();
            target.colorId = 2;
            target.maxFill = 3;
            target.currentFill = 2;
            target.RefreshVisuals();

            GameObject sourceObject = new GameObject("Release source bucket");
            sourceObject.AddComponent<SpriteRenderer>();
            Bucket source = sourceObject.AddComponent<Bucket>();
            source.colorId = 9;
            source.maxFill = 3;
            Dot dot = CreateDot(2, Color.blue);
            SeedContainedDot(source, dot);

            Assert.That(source.ReleaseContents(), Is.True);

            Assert.That(_manager.MovesLeft, Is.EqualTo(0));
            Assert.That(_manager.HasLost, Is.False);
            Assert.That(_manager.CanUseMove, Is.False);

            Object.DestroyImmediate(dot.gameObject);
            Object.DestroyImmediate(sourceObject);
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(fallingObject);
        }

        [Test]
        public void RowFillProgress_FillsBottomRowsBeforeTopRows()
        {
            Assert.That(SpriteGridFill.GetBottomUpRowProgress(0.25f, 0, 4), Is.EqualTo(1f));
            Assert.That(SpriteGridFill.GetBottomUpRowProgress(0.25f, 1, 4), Is.EqualTo(0f));
            Assert.That(SpriteGridFill.GetBottomUpRowProgress(0.625f, 2, 4), Is.EqualTo(0.5f));
            Assert.That(SpriteGridFill.GetBottomUpRowProgress(0.625f, 3, 4), Is.EqualTo(0f));
        }

        static Dot CreateDot(int colorId, Color color)
        {
            GameObject dotObject = new GameObject($"Dot {colorId}");
            dotObject.AddComponent<SpriteRenderer>();
            Dot dot = dotObject.AddComponent<Dot>();
            dot.colorId = colorId;
            dot.color = color;
            return dot;
        }

        static void SeedContainedDot(Bucket bucket, Dot dot)
        {
            FieldInfo containedField = typeof(Bucket).GetField(
                "_contained",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo containedColorField = typeof(Bucket).GetField(
                "_containedColorId",
                BindingFlags.Instance | BindingFlags.NonPublic);

            List<Dot> contained = (List<Dot>)containedField.GetValue(bucket);
            contained.Add(dot);
            containedColorField.SetValue(bucket, dot.colorId);
            bucket.currentFill = 1;
            bucket.RefreshVisuals();
        }

        static void SetGamePlayManagerInstance(GamePlayManager instance)
        {
            PropertyInfo property = typeof(GamePlayManager).GetProperty(
                "Instance",
                BindingFlags.Static | BindingFlags.Public);
            property.SetValue(null, instance);
        }
    }
}
