namespace HAVIGAME {

    [System.Serializable]
    public abstract class Value<T> {
        public abstract T Get(Args args);
        public abstract bool Set(T value, Args args);
    }
}