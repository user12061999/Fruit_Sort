using UnityEngine;

namespace HAVIGAME.Audios {

    [SettingMenu(typeof(AudioSettings), "Generic/Audio", "", null, 2, "Icons/icon_audio.psd")]
    [CreateAssetMenu(menuName = "HAVIGAME/Settings/Audio", fileName = "AudioSettings")]
    public class AudioSettings : Settings<AudioSettings> {

        [SerializeField] private float fadeSpeed = 1f;
        [SerializeField] private string saveId = "audio";

        [Header("[Channels]")]
        [SerializeReference] private AudioSource ambient;
        [SerializeReference] private AudioSource music;
        [SerializeReference] private AudioSource soundFX;

        public float FadeSpeed => fadeSpeed;
        public string SaveId => saveId;
        public AudioSource Ambient => ambient;
        public AudioSource Music => music;
        public AudioSource SoundFX => soundFX;
    }
}
