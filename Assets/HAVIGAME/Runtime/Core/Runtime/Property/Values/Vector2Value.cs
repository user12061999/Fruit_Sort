using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public abstract class Vector2Value : Value<Vector2> { }


    [System.Serializable, CategoryMenu("Vector2", -100)]
    public class DefaultVector2Value : Vector2Value {
        [SerializeField] private Vector2 value;

        public override Vector2 Get(Args args) {
            return this.value;
        }

        public override bool Set(Vector2 value, Args args) {
            if (this.value != value) {
                this.value = value;
                return true;
            }
            else {
                return false;
            }
        }
    }


    [System.Serializable, CategoryMenu("Constant/Zero", -100)]
    public class ZeroVector2Value : Vector2Value {
        public override Vector2 Get(Args args) {
            return Vector2.zero;
        }

        public override bool Set(Vector2 value, Args args) {
            return false;
        }
    }

    [System.Serializable, CategoryMenu("Constant/One", -100)]
    public class OneVector2Value : Vector2Value {
        public override Vector2 Get(Args args) {
            return Vector2.one;
        }

        public override bool Set(Vector2 value, Args args) {
            return false;
        }
    }

    [System.Serializable, CategoryMenu("Constant/Up", -100)]
    public class UpVector2Value : Vector2Value {
        public override Vector2 Get(Args args) {
            return Vector2.up;
        }

        public override bool Set(Vector2 value, Args args) {
            return false;
        }
    }

    [System.Serializable, CategoryMenu("Constant/Down", -100)]
    public class DownVector2Value : Vector2Value {
        public override Vector2 Get(Args args) {
            return Vector2.down;
        }

        public override bool Set(Vector2 value, Args args) {
            return false;
        }
    }

    [System.Serializable, CategoryMenu("Constant/Left", -100)]
    public class LeftVector2Value : Vector2Value {
        public override Vector2 Get(Args args) {
            return Vector2.left;
        }

        public override bool Set(Vector2 value, Args args) {
            return false;
        }
    }

    [System.Serializable, CategoryMenu("Constant/Right", -100)]
    public class RightVector2Value : Vector2Value {
        public override Vector2 Get(Args args) {
            return Vector2.right;
        }

        public override bool Set(Vector2 value, Args args) {
            return false;
        }
    }


    [System.Serializable, CategoryMenu("Screen/Size")]
    public class ScreenSizeValue : Vector2Value {
        public override Vector2 Get(Args args) {
            return new Vector2(Screen.width, Screen.height);
        }

        public override bool Set(Vector2 value, Args args) {
            return false;
        }
    }

    [System.Serializable, CategoryMenu("Random/Inside Unit Circle")]
    public class RandomInsideUnitCircleValue : Vector2Value {
        public override Vector2 Get(Args args) {
            return Random.insideUnitCircle;
        }

        public override bool Set(Vector2 value, Args args) {
            return false;
        }
    }
}
