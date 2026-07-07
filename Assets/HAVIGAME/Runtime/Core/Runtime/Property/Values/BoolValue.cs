using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public abstract class BoolValue : Value<bool> { }


    [System.Serializable]
    [CategoryMenu("Bool", -100)]
    public class DefaultBoolValue : BoolValue {
        [SerializeField] private bool value;

        public override bool Get(Args args) {
            return this.value;
        }

        public override bool Set(bool value, Args args) {
            if (this.value != value) {
                this.value = value;
                return true;
            }
            else {
                return false;
            }
        }
    }


    [System.Serializable]
    [CategoryMenu("Application/Is Playing")]
    public class ApplicationIsPlayingBoolValue : BoolValue {
        public override bool Get(Args args) {
            return Application.isPlaying;
        }

        public override bool Set(bool value, Args args) {
            return false;
        }
    }
}