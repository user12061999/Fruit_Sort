using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public abstract class FloatValue : Value<float> { }


    [System.Serializable]
    [CategoryMenu("Float", -100)]
    public class DefaultFloatValue : FloatValue {
        [SerializeField] private float value;

        public override float Get(Args args) {
            return this.value;
        }

        public override bool Set(float value, Args args) {
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
    [CategoryMenu("Screen/DPI")]
    public class SreenDPIFloatValue : FloatValue {
        public override float Get(Args args) {
            return Screen.dpi;
        }

        public override bool Set(float value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Screen/Brightness")]
    public class SreenBrightnessFloatValue : FloatValue {
        public override float Get(Args args) {
            return Screen.brightness;
        }

        public override bool Set(float value, Args args) {
            if (Screen.brightness != value) {
                Screen.brightness = value;
                return true;
            }
            else {
                return false;
            }
        }
    }

    [System.Serializable]
    [CategoryMenu("Screen/Width")]
    public class SreenWidthFloatValue : FloatValue {
        public override float Get(Args args) {
            return Screen.width;
        }

        public override bool Set(float value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Screen/Height")]
    public class SreenHeightFloatValue : FloatValue {
        public override float Get(Args args) {
            return Screen.height;
        }

        public override bool Set(float value, Args args) {
            return false;
        }
    }


    [System.Serializable]
    [CategoryMenu("Random/Value")]
    public class RandomFloatValue : FloatValue {
        public override float Get(Args args) {
            return Random.value;
        }

        public override bool Set(float value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Random/Range")]
    public class RandomRangeFloatValue : FloatValue {
        [SerializeField] private float min;
        [SerializeField] private float max;

        public override float Get(Args args) {
            return Random.Range(min, max);
        }

        public override bool Set(float value, Args args) {
            return false;
        }
    }
}