using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public abstract class GameObjectValue : Value<GameObject> { }


    [System.Serializable]
    [CategoryMenu("GameObject", -100)]
    public class DefaultGameObjectValue : GameObjectValue {
        [SerializeField] private GameObject value;

        public override GameObject Get(Args args) {
            return this.value;
        }

        public override bool Set(GameObject value, Args args) {
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
    public class SelfGameObjectValue : GameObjectValue {
        public override GameObject Get(Args args) {
            return args.Self;
        }

        public override bool Set(GameObject value, Args args) {
            return false;
        }
    }


    [System.Serializable]
    [CategoryMenu("Target", -99)]
    public class TargetGameObjectValue : GameObjectValue {
        public override GameObject Get(Args args) {
            return args.Target;
        }

        public override bool Set(GameObject value, Args args) {
            return false;
        }
    }
}