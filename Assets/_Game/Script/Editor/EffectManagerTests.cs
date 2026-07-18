using NUnit.Framework;
using UnityEngine;

namespace FruitSort.EditorTests
{
    public sealed class EffectManagerTests
    {
        GameObject _managerObject;
        EffectManager _manager;
        EffectLibrary _library;

        [SetUp]
        public void SetUp()
        {
            _managerObject = new GameObject("Effect manager under test");
            _manager = _managerObject.AddComponent<EffectManager>();
            _library = ScriptableObject.CreateInstance<EffectLibrary>();
            _manager.SetEffectLibrary(_library);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_managerObject);
            Object.DestroyImmediate(_library);
        }

        [Test]
        public void Configure_UsesLatestDefinitionForEachEffectType()
        {
            _library.Configure(new[]
            {
                new EffectDefinition { effectType = GameEffectType.DotHit, soundDelay = 0.1f },
                new EffectDefinition { effectType = GameEffectType.DotHit, soundDelay = 0.3f }
            });

            Assert.That(_manager.TryGetDefinition(GameEffectType.DotHit, out EffectDefinition definition), Is.True);
            Assert.That(definition.soundDelay, Is.EqualTo(0.3f));
        }

        [Test]
        public void Configure_NormalizesInvalidTimingAndVolume()
        {
            _library.Configure(new[]
            {
                new EffectDefinition
                {
                    effectType = GameEffectType.BucketComplete,
                    soundDelay = -1f,
                    volume = 2f,
                    cleanupDelay = -2f
                }
            });

            _manager.TryGetDefinition(GameEffectType.BucketComplete, out EffectDefinition definition);

            Assert.That(definition.soundDelay, Is.EqualTo(0f));
            Assert.That(definition.volume, Is.EqualTo(1f));
            Assert.That(definition.cleanupDelay, Is.EqualTo(0f));
        }

        [Test]
        public void Play_ReturnsFalseWhenEffectHasNotBeenConfigured()
        {
            Assert.That(_manager.Play(GameEffectType.LevelWin, Vector3.zero), Is.False);
        }
    }
}
