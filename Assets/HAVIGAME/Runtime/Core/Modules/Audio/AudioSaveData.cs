using UnityEngine;
using HAVIGAME.SaveLoad;

namespace HAVIGAME.Audios {
    [System.Serializable]
    public class AudioSaveData : SaveData {
        [SerializeField] private float totalVolume;
        [SerializeField] private float ambientVolume;
        [SerializeField] private float musicVolume;
        [SerializeField] private float soundFXVolume;
        [SerializeField] private bool vibrateEnabled;

        public float TotalVolume => totalVolume;
        public float AmbientVolume => ambientVolume;
        public float MusicVolume => musicVolume;
        public float SoundFXVolume => soundFXVolume;
        public bool VibrateEnabled => vibrateEnabled;

        public AudioSaveData() {
            totalVolume = 0.75f;
            ambientVolume = 1f;
            musicVolume = 1f;
            soundFXVolume = 1f;
            vibrateEnabled = true;
        }

        public void SetTotalVolume(float totalVolume) {
            if (this.totalVolume != totalVolume) {
                this.totalVolume = totalVolume;

                SetChanged();
            }
        }

        public void SetAmbientVolume(float ambientVolume) {
            if (this.ambientVolume != ambientVolume) {
                this.ambientVolume = ambientVolume;

                SetChanged();
            }
        }

        public void SetMusicVolume(float musicVolume) {
            if (this.musicVolume != musicVolume) {
                this.musicVolume = musicVolume;

                SetChanged();
            }
        }

        public void SetSoundFXVolume(float soundFXVolume) {
            if (this.soundFXVolume != soundFXVolume) {
                this.soundFXVolume = soundFXVolume;

                SetChanged();
            }
        }

        public void SetVibrateEnabled(bool vibrateEnabled) {
            if (this.vibrateEnabled != vibrateEnabled) {
                this.vibrateEnabled = vibrateEnabled;

                SetChanged();
            }
        }
    }
}
