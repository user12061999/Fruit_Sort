namespace HAVIGAME {

    [System.Serializable]
    public sealed class BoolProperty : Property<BoolValue, bool> {
        public static BoolProperty Create() {
            return new BoolProperty(new DefaultBoolValue());
        }

        public BoolProperty(BoolValue value) : base(value) {

        }
    }

    [System.Serializable]
    public sealed class BoolPropertyReadonly : PropertyReadonly<BoolValue, bool> {
        public static BoolPropertyReadonly Create() {
            return new BoolPropertyReadonly(new DefaultBoolValue());
        }

        public BoolPropertyReadonly(BoolValue value) : base(value) {

        }
    }
}