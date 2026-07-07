using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public class AudioClipProperty : Property<AudioClipValue, AudioClip> {
        public static AudioClipProperty Create() {
            return new AudioClipProperty(new DefaultAudioClipValue());
        }

        public AudioClipProperty(AudioClipValue value) : base(value) {

        }
    }

    [System.Serializable]
    public sealed class AudioClipPropertyReadonly : PropertyReadonly<AudioClipValue, AudioClip> {
        public static AudioClipPropertyReadonly Create() {
            return new AudioClipPropertyReadonly(new DefaultAudioClipValue());
        }

        public AudioClipPropertyReadonly(AudioClipValue value) : base(value) {

        }
    }
}