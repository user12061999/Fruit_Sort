namespace HAVIGAME {

    [System.Serializable]
    public sealed class IntProperty : Property<IntValue, int> {
        public static IntProperty Create() {
            return new IntProperty(new DefaultIntValue());
        }

        public IntProperty(IntValue value) : base(value) {

        }
    }

    [System.Serializable]
    public sealed class IntPropertyReadonly : PropertyReadonly<IntValue, int> {
        public static IntPropertyReadonly Create() {
            return new IntPropertyReadonly(new DefaultIntValue());
        }

        public IntPropertyReadonly(IntValue value) : base(value) {

        }
    }
}