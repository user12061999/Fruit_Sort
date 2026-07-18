using HAVIGAME.Audios;
using UnityEngine;

namespace FruitSort
{
    public enum GameMusicTrack
    {
        Menu,
        Gameplay,
        Win,
        Lose
    }

    [CreateAssetMenu(fileName = "GameAudioLibrary", menuName = "FruitSort/Game Audio Library")]
    public sealed class GameAudioLibrary : ScriptableObject
    {
        public static GameAudioLibrary Active { get; private set; }

        [Header("Gameplay SFX")]
        [SerializeField] Audio buttonClick;
        [SerializeField] Audio screenClick;
        [SerializeField] Audio dotDestroyed;
        [SerializeField] Audio dotLand;
        [SerializeField] Audio bucketReceive;
        [SerializeField] Audio bucketComplete;
        [SerializeField] Audio bucketRelease;

        [Header("Music")]
        [SerializeField] Audio menuMusic;
        [SerializeField] Audio gameplayMusic;
        [SerializeField] Audio winMusic;
        [SerializeField] Audio loseMusic;

        public void Activate()
        {
            Active = this;
        }

        public bool PlayButtonClick()
        {
            if (buttonClick == null || buttonClick.IsEmpty) return false;
            buttonClick.Play();
            return true;
        }

        public bool PlayEffect(GameEffectType effectType)
        {
            Audio audio = GetEffectSound(effectType);
            if (audio == null || audio.IsEmpty) return false;
            audio.Play();
            return true;
        }

        public bool PlayMusic(GameMusicTrack track)
        {
            Audio audio = GetMusic(track);
            if (audio == null || audio.IsEmpty) return false;
            audio.Play();
            return true;
        }

        public void StopMusic()
        {
            if (AudioManager.HasInstance)
                AudioManager.Instance.Stop(AudioChannel.Music);
        }

        public Audio GetEffectSound(GameEffectType effectType)
        {
            switch (effectType)
            {
                case GameEffectType.DotDestroyed: return dotDestroyed;
                case GameEffectType.DotLand: return dotLand;
                case GameEffectType.BucketReceive: return bucketReceive;
                case GameEffectType.BucketComplete: return bucketComplete;
                case GameEffectType.BucketRelease: return bucketRelease;
                case GameEffectType.ScreenClick: return screenClick;
                default: return null;
            }
        }

        public Audio GetMusic(GameMusicTrack track)
        {
            switch (track)
            {
                case GameMusicTrack.Menu: return menuMusic;
                case GameMusicTrack.Gameplay: return gameplayMusic;
                case GameMusicTrack.Win: return winMusic;
                case GameMusicTrack.Lose: return loseMusic;
                default: return null;
            }
        }
    }
}
