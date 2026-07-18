using NUnit.Framework;
using UnityEngine;

namespace FruitSort.EditorTests
{
    public sealed class GameAudioLibraryTests
    {
        GameAudioLibrary _library;

        [SetUp]
        public void SetUp()
        {
            _library = ScriptableObject.CreateInstance<GameAudioLibrary>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_library);
        }

        [Test]
        public void EmptyLibrary_DoesNotPlayUnconfiguredEffectOrMusic()
        {
            Assert.That(_library.PlayEffect(GameEffectType.DotDestroyed), Is.False);
            Assert.That(_library.PlayMusic(GameMusicTrack.Gameplay), Is.False);
        }

        [Test]
        public void UnknownKeys_ReturnNoAudio()
        {
            Assert.That(_library.GetEffectSound(GameEffectType.LevelWin), Is.Null);
            Assert.That(_library.GetMusic((GameMusicTrack)99), Is.Null);
        }
    }
}
