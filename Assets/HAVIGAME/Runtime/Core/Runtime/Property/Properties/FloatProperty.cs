namespace HAVIGAME {

    [System.Serializable]
    public sealed class FloatProperty : Property<FloatValue, float> {
        public static FloatProperty Create() {
            return new FloatProperty(new DefaultFloatValue());
        }

        public FloatProperty(FloatValue value) : base(value) {

        }
    }

    [System.Serializable]
    public sealed class FloatPropertyReadonly : PropertyReadonly<FloatValue, float> {
        public static FloatPropertyReadonly Create() {
            return new FloatPropertyReadonly(new DefaultFloatValue());
        }

        public FloatPropertyReadonly(FloatValue value) : base(value) {

        }
    }
}