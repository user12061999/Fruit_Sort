using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public abstract class IntValue : Value<int> { }


    [System.Serializable]
    [CategoryMenu("Int", -100)]
    public class DefaultIntValue : IntValue {
        [SerializeField] private int value;

        public override int Get(Args args) {
            return this.value;
        }

        public override bool Set(int value, Args args) {
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
    [CategoryMenu("Screen/Width")]
    public class SreenWidthIntValue : IntValue {
        public override int Get(Args args) {
            return Screen.width;
        }

        public override bool Set(int value, Args args) {
            return false;
        }
    }


    [System.Serializable]
    [CategoryMenu("Screen/Height")]
    public class SreenHeightIntValue : IntValue {
        public override int Get(Args args) {
            return Screen.height;
        }

        public override bool Set(int value, Args args) {
            return false;
        }
    }


    [System.Serializable]
    [CategoryMenu("Random/Range")]
    public class RandomRangeIntValue : IntValue {
        [SerializeField] private int min;
        [SerializeField] private int max;

        public override int Get(Args args) {
            return Random.Range(min, max);
        }

        public override bool Set(int value, Args args) {
            return false;
        }
    }
}