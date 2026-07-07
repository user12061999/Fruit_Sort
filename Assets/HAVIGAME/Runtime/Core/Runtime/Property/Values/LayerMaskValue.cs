using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public abstract class LayerMaskValue : Value<LayerMask> { }


    [System.Serializable]
    [CategoryMenu("Layer Mask", -100)]
    public class DefaultLayerMaskValue : LayerMaskValue {
        [SerializeField] private LayerMask value;

        public override LayerMask Get(Args args) {
            return this.value;
        }

        public override bool Set(LayerMask value, Args args) {
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