using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public abstract class AudioClipValue : Value<AudioClip> { }


    [System.Serializable]
    [CategoryMenu("AudioClip", -100)]
    public class DefaultAudioClipValue : AudioClipValue {
        [SerializeField] private AudioClip value;

        public override AudioClip Get(Args args) {
            return this.value;
        }

        public override bool Set(AudioClip value, Args args) {
            if (this.value != value) {
                this.value = value;
                return true;
            }
            else {
                return false;
            }
        }
    }
}