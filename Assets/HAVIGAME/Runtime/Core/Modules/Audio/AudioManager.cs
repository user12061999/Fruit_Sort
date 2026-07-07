using HAVIGAME.SaveLoad;
using UnityEngine;

namespace HAVIGAME.Audios {

    public class AudioManager : Singleton<AudioManager> {
        public static readonly InitializeEvent initializeEvent = new InitializeEvent();
        public static event AudioEvent onVolumeChanged;
        public static event AudioEvent onAmbientVolumeChanged;
        public static event AudioEvent onMusicVolumeChanged;
        public static event AudioEvent onSoundFXVolumeChanged;

        private AudioPlayer ambientPlayer;
        private AudioPlayer musicPlayer;
        private AudioPlayer soundFXPlayer;
        private DataHolder<AudioSaveData> dataHolder;

        public float TotalVolume {
            get {
                return dataHolder.Data.TotalVolume;
            }
            set {
                if (dataHolder.Data.TotalVolume != value) {
                    dataHolder.Data.SetTotalVolume(value);

                    if (ambientPlayer != null) ambientPlayer.TotalVolume = value;
                    if (musicPlayer != null) musicPlayer.TotalVolume = value;
                    if (soundFXPlayer != null) soundFXPlayer.TotalVolume = value;

                    onVolumeChanged?.Invoke(TotalVolume);
                }
            }
        }

        public float AmbientVolume {
            get {
                return dataHolder.Data.AmbientVolume;
            }
            set {
                if (dataHolder.Data.AmbientVolume != value) {
                    dataHolder.Data.SetAmbientVolume(value);

                    if (ambientPlayer != null) ambientPlayer.ChannelVolume = value;

                    onAmbientVolumeChanged?.Invoke(AmbientVolume);
                }
            }
        }

        public float MusicVolume {
            get {
                return dataHolder.Data.MusicVolume;
            }
            set {
                if (dataHolder.Data.MusicVolume != value) {
                    dataHolder.Data.SetMusicVolume(value);

                    if (musicPlayer != null) musicPlayer.ChannelVolume = value;

                    onMusicVolumeChanged?.Invoke(MusicVolume);
                }
            }
        }

        public float SoundFXVolume {
            get {
                return dataHolder.Data.SoundFXVolume;
            }
            set {
                if (dataHolder.Data.SoundFXVolume != value) {
                    dataHolder.Data.SetSoundFXVolume(value);

                    if (soundFXPlayer != null) soundFXPlayer.ChannelVolume = value;

                    onSoundFXVolumeChanged?.Invoke(SoundFXVolume);
                }
            }
        }

        public bool VibrateEnabled {
            get {
                return dataHolder.Data.VibrateEnabled;
            }
            set {
                dataHolder.Data.SetVibrateEnabled(value);
            }
        }

        protected override void OnAwake() {
            base.OnAwake();

            AudioSettings settings = AudioSettings.Instance;

            dataHolder = SaveLoadManager.Create<AudioSaveData>(settings.SaveId);

            if (settings.Ambient) {
                AudioSource ambient = Instantiate(settings.Ambient);
                ambient.transform.SetParent(transform);
                ambientPlayer = new AudioPlayer(ambient, TotalVolume, settings.FadeSpeed, AudioChannel.Ambient, AmbientVolume);
            }

            if (settings.Music) {
                AudioSource music = Instantiate(settings.Music);
                music.transform.SetParent(transform);
                musicPlayer = new AudioPlayer(music, TotalVolume, settings.FadeSpeed, AudioChannel.Music, MusicVolume);
            }

            if (settings.SoundFX) {
                AudioSource soundFX = Instantiate(settings.SoundFX);
                soundFX.transform.SetParent(transform);
                soundFXPlayer = new AudioPlayer(soundFX, TotalVolume, settings.FadeSpeed, AudioChannel.SoundFX, SoundFXVolume);
            }

            Database.Unload(settings);

            Log.Debug("[AudioManager] Initialize completed.");
            initializeEvent.Invoke(true);
        }

        public void Play(Audio audio) {
            if (audio == null) {
                return;
            }

            switch (audio.Channel) {
                case AudioChannel.Ambient:
                    if (ambientPlayer != null) {
                        ambientPlayer.Play(audio);
                    }
                    break;
                case AudioChannel.Music:
                    if (musicPlayer != null) {
                        musicPlayer.Play(audio);
                    }
                    break;
                case AudioChannel.SoundFX:
                    if (soundFXPlayer != null) {
                        soundFXPlayer.PlayOnShot(audio);
                    }
                    break;
            }
        }

        public void PlayAtPosition(Audio audio, Vector3 position) {
            if (audio == null) {
                return;
            }

            switch (audio.Channel) {
                case AudioChannel.Ambient:
                    if (ambientPlayer != null) {
                        ambientPlayer.PlayAtPosition(audio, position);
                    }
                    break;
                case AudioChannel.Music:
                    if (musicPlayer != null) {
                        musicPlayer.PlayAtPosition(audio, position);
                    }
                    break;
                case AudioChannel.SoundFX:
                    if (soundFXPlayer != null) {
                        soundFXPlayer.PlayAtPosition(audio, position);
                    }
                    break;
            }
        }

        public void PlayAtPosition(AudioClip audioClip, AudioChannel channel, Vector3 position) {
            if (audioClip == null) {
                return;
            }

            switch (channel) {
                case AudioChannel.Ambient:
                    if (ambientPlayer != null) {
                        ambientPlayer.PlayAtPosition(audioClip, position);
                    }
                    break;
                case AudioChannel.Music:
                    if (musicPlayer != null) {
                        musicPlayer.PlayAtPosition(audioClip, position);
                    }
                    break;
                case AudioChannel.SoundFX:
                    if (soundFXPlayer != null) {
                        soundFXPlayer.PlayAtPosition(audioClip, position);
                    }
                    break;
            }
        }

        public void Stop(AudioChannel channel) {
            switch (channel) {
                case AudioChannel.Ambient:
                    if (ambientPlayer != null) {
                        ambientPlayer.Stop();
                    }
                    break;
                case AudioChannel.Music:
                    if (musicPlayer != null) {
                        musicPlayer.Stop();
                    }
                    break;
                case AudioChannel.SoundFX:
                    if (soundFXPlayer != null) {
                        soundFXPlayer.Stop();
                    }
                    break;
            }
        }

        public void Vibrate() {
            if (VibrateEnabled) {
                Handheld.Vibrate();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {

            public override int Order => EXTEND_MODULE;
            public override InitializeEvent InitializeEvent => AudioManager.initializeEvent;

            public override void Initialize() {
                AudioManager.Instance.Create();
            }
        }
    }
}
