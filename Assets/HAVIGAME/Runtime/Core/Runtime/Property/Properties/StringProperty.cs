namespace HAVIGAME {

    [System.Serializable]
    public sealed class StringProperty : Property<StringValue, string> {
        public static StringProperty Create() {
            return new StringProperty(new DefaultStringValue());
        }

        public StringProperty(StringValue value) : base(value) {

        }
    }

    [System.Serializable]
    public sealed class StringPropertyReadonly : PropertyReadonly<StringValue, string> {
        public static StringPropertyReadonly Create() {
            return new StringPropertyReadonly(new DefaultStringValue());
        }

        public StringPropertyReadonly(StringValue value) : base(value) {

        }
    }
}