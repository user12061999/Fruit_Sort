namespace HAVIGAME {

    using UnityEngine;

    [System.Serializable]
    public sealed class LayerMaskProperty : Property<LayerMaskValue, LayerMask> {
        public static LayerMaskProperty Create() {
            return new LayerMaskProperty(new DefaultLayerMaskValue());
        }

        public LayerMaskProperty(LayerMaskValue value) : base(value) {

        }
    }

    [System.Serializable]
    public sealed class LayerMaskPropertyReadonly : PropertyReadonly<LayerMaskValue, LayerMask> {
        public static LayerMaskPropertyReadonly Create() {
            return new LayerMaskPropertyReadonly(new DefaultLayerMaskValue());
        }

        public LayerMaskPropertyReadonly(LayerMaskValue value) : base(value) {

        }
    }
}