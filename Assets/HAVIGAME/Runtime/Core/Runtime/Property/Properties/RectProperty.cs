using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public class RectProperty : Property<RectValue, Rect> {
        public static RectProperty Create() {
            return new RectProperty(new DefaultRectValue());
        }

        public RectProperty(RectValue value) : base(value) {

        }
    }

    [System.Serializable]
    public sealed class RectPropertyReadonly : PropertyReadonly<RectValue, Rect> {
        public static RectPropertyReadonly Create() {
            return new RectPropertyReadonly(new DefaultRectValue());
        }

        public RectPropertyReadonly(RectValue value) : base(value) {

        }
    }
}