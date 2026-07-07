using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public class TransformProperty : Property<TransformValue, Transform> {
        public static TransformProperty Create() {
            return new TransformProperty(new DefaultTransformValue());
        }

        public TransformProperty(TransformValue value) : base(value) {

        }
    }

    [System.Serializable]
    public sealed class TransformPropertyReadonly : PropertyReadonly<TransformValue, Transform> {
        public static TransformPropertyReadonly Create() {
            return new TransformPropertyReadonly(new DefaultTransformValue());
        }

        public TransformPropertyReadonly(TransformValue value) : base(value) {

        }
    }
}