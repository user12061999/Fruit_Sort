using UnityEngine;

namespace HAVIGAME.Audios {
    [RequireComponent(typeof(AudioSource))]
    public class AudioSourceControl : MonoBehaviour {
        [SerializeField] private AudioChannel followChannel = AudioChannel.SoundFX;

        private AudioSource audioSource;
        private float startVolume;

        private void Awake() {
            audioSource = GetComponent<AudioSource>();
            startVolume = audioSource.volume;
        }

        private void Start() {
            OnVolumeChanged(AudioManager.Instance.TotalVolume);

            AudioManager.onVolumeChanged += OnVolumeChanged;

            switch (followChannel) {
                case AudioChannel.Ambient:
                    AudioManager.onAmbientVolumeChanged += OnAmbientVolumeChanged;
                    break;
                case AudioChannel.Music:
                    AudioManager.onMusicVolumeChanged += OnMusicVolumeChanged;
                    break;
                case AudioChannel.SoundFX:
                    AudioManager.onSoundFXVolumeChanged += OnSoundFXVolumeChanged;
                    break;
            }
        }

        private void OnDestroy() {
            AudioManager.onVolumeChanged -= OnVolumeChanged;

            switch (followChannel) {
                case AudioChannel.Ambient:
                    AudioManager.onAmbientVolumeChanged -= OnAmbientVolumeChanged;
                    break;
                case AudioChannel.Music:
                    AudioManager.onMusicVolumeChanged -= OnMusicVolumeChanged;
                    break;
                case AudioChannel.SoundFX:
                    AudioManager.onSoundFXVolumeChanged -= OnSoundFXVolumeChanged;
                    break;
            }
        }

        private void OnVolumeChanged(float volume) {
            audioSource.volume = startVolume * AudioManager.Instance.TotalVolume * volume;
        }

        private void OnAmbientVolumeChanged(float volume) {
            audioSource.volume = startVolume * volume;
        }

        private void OnMusicVolumeChanged(float volume) {
            audioSource.volume = startVolume * volume;
        }

        private void OnSoundFXVolumeChanged(float volume) {
            audioSource.volume = startVolume * volume;
        }

        private void UpdateAudioSource() {
            AudioManager audioManager = AudioManager.Instance;

            switch (followChannel) {
                case AudioChannel.Ambient:
                    audioSource.volume = startVolume * audioManager.TotalVolume * audioManager.AmbientVolume;
                    break;
                case AudioChannel.Music:
                    audioSource.volume = startVolume * audioManager.TotalVolume * audioManager.MusicVolume;
                    break;
                case AudioChannel.SoundFX:
                    audioSource.volume = startVolume * audioManager.TotalVolume * audioManager.SoundFXVolume;
                    break;
            }
        }
    }
}
