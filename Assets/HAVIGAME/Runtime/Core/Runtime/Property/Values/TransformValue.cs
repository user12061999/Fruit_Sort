using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public abstract class TransformValue : Value<Transform> { }


    [System.Serializable]
    [CategoryMenu("Transform", -100)]
    public class DefaultTransformValue : TransformValue {
        [SerializeField] private Transform value;

        public override Transform Get(Args args) {
            return this.value;
        }

        public override bool Set(Transform value, Args args) {
            if (this.value != value) {
                this.value = value;
                return true;
            }
            else {
                return false;
            }
        }
    }


    [System.Serializable]
    [CategoryMenu("Self", -99)]
    public class SelfTransformValue : TransformValue {
        public override Transform Get(Args args) {
            return args.Self.transform;
        }

        public override bool Set(Transform value, Args args) {
            return false;
        }
    }


    [System.Serializable]
    [CategoryMenu("Target", -99)]
    public class TargetTransformValue : TransformValue {
        public override Transform Get(Args args) {
            return args.Target.transform;
        }

        public override bool Set(Transform value, Args args) {
            return false;
        }
    }
}