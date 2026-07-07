using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public sealed class ColorProperty : Property<ColorValue, Color> {
        public static ColorProperty Create() {
            return new ColorProperty(new DefaultColorValue());
        }

        public ColorProperty(ColorValue value) : base(value) {

        }
    }

    [System.Serializable]
    public sealed class ColorPropertyReadonly : PropertyReadonly<ColorValue, Color> {
        public static ColorPropertyReadonly Create() {
            return new ColorPropertyReadonly(new DefaultColorValue());
        }

        public ColorPropertyReadonly(ColorValue value) : base(value) {

        }
    }
}