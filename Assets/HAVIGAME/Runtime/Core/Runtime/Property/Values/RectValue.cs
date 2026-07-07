using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public abstract class RectValue : Value<Rect> { }


    [System.Serializable]
    [CategoryMenu("Rect", -100)]
    public class DefaultRectValue : RectValue {
        [SerializeField] private Rect value;

        public override Rect Get(Args args) {
            return this.value;
        }

        public override bool Set(Rect value, Args args) {
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