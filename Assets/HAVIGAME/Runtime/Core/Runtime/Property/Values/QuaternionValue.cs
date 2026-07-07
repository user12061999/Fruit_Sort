using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public abstract class QuaternionValue : Value<Quaternion> { }


    [System.Serializable]
    [CategoryMenu("Quaternion", -100)]
    public class DefaultQuaternionValue : QuaternionValue {
        [SerializeField] private Quaternion value;

        public override Quaternion Get(Args args) {
            return this.value;
        }

        public override bool Set(Quaternion value, Args args) {
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
    public class SelfQuaternionValue : QuaternionValue {
        public override Quaternion Get(Args args) {
            return args.Self.transform.rotation;
        }

        public override bool Set(Quaternion value, Args args) {
            return false;
        }
    }


    [System.Serializable]
    [CategoryMenu("Target", -99)]
    public class TargetQuaternionValue : QuaternionValue {
        public override Quaternion Get(Args args) {
            return args.Target.transform.rotation;
        }

        public override bool Set(Quaternion value, Args args) {
            return false;
        }
    }
}