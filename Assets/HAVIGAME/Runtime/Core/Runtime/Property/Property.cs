using UnityEngine;

namespace HAVIGAME {

    public interface IPropertyValue {
        bool IsNull { get; }
        bool IsReadonly { get; }
    }

    [System.Serializable]
    public class PropertyReadonly<TValue, T> : IPropertyValue where TValue : Value<T> {
        [SerializeReference] protected TValue value;

        public bool IsNull => value == null;
        public virtual bool IsReadonly => true;

        public PropertyReadonly(TValue value) {
            this.value = value;
        }


        public T Get() {
            Args args = Args.EMPTY;
            T result = Get(args);
            return result;
        }

        public T Get(GameObject self) {
            Args args = ReferencePool.Acquire<Args>();
            args.ChangeSelf(self);

            T result = Get(args);

            ReferencePool.Release(args);

            return result;
        }

        public T Get(GameObject self, GameObject target) {
            Args args = ReferencePool.Acquire<Args>();
            args.ChangeSelf(self);
            args.ChangeTarget(target);

            T result = Get(args);

            ReferencePool.Release(args);

            return result;
        }

        public T Get(Component self) {
            Args args = ReferencePool.Acquire<Args>();
            args.ChangeSelf(self);

            T result = Get(args);

            ReferencePool.Release(args);

            return result;
        }

        public T Get(Component self, Component target) {
            Args args = ReferencePool.Acquire<Args>();
            args.ChangeSelf(self);
            args.ChangeTarget(target);

            T result = Get(args);

            ReferencePool.Release(args);

            return result;
        }

        public T Get(Args args) {
            return value.Get(args);
        }
    }


    [System.Serializable]
    public class Property<TValue, T> : PropertyReadonly<TValue, T> where TValue : Value<T> {
        public delegate void PropertyDelegate(T value, Args args);

        public event PropertyDelegate onValueChanged;

        public override bool IsReadonly => false;

        public Property(TValue value) : base(value) {

        }

        public void Set(T value) {
            Args args = Args.EMPTY;

            Set(value, args);
        }

        public void Set(T value, GameObject self) {
            Args args = ReferencePool.Acquire<Args>();
            args.ChangeSelf(self);

            Set(value, args);

            ReferencePool.Release(args);
        }

        public void Set(T value, GameObject self, GameObject target) {
            Args args = ReferencePool.Acquire<Args>();
            args.ChangeSelf(self);
            args.ChangeSelf(target);

            Set(value, args);

            ReferencePool.Release(args);
        }

        public void Set(T value, Component self) {
            Args args = ReferencePool.Acquire<Args>();
            args.ChangeSelf(self);

            Set(value, args);

            ReferencePool.Release(args);
        }

        public void Set(T value, Component self, GameObject target) {
            Args args = ReferencePool.Acquire<Args>();
            args.ChangeSelf(self);
            args.ChangeSelf(target);

            Set(value, args);

            ReferencePool.Release(args);
        }

        public void Set(T value, Args args) {
            if (this.value.Set(value, args)) {
                onValueChanged?.Invoke(value, args);
            }
        }
    }
}