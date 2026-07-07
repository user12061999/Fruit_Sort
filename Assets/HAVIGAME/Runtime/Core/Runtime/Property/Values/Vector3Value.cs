using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public abstract class Vector3Value : Value<Vector3> { }

    [System.Serializable]
    [CategoryMenu("Vector3", -100)]
    public class DefaultVector3Value : Vector3Value {
        [SerializeField] private Vector3 value;

        public override Vector3 Get(Args args) {
            return this.value;
        }

        public override bool Set(Vector3 value, Args args) {
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
    public class SelfPositionValue : Vector3Value {
        [SerializeField] private Space space;

        public override Vector3 Get(Args args) {
            switch (space) {
                case Space.Self:
                    return args.Self.transform.localPosition;
                default:
                    return args.Self.transform.position;
            }
        }

        public override bool Set(Vector3 value, Args args) {
            switch (space) {
                case Space.Self:
                    if (args.Self.transform.localPosition != value) {
                        args.Self.transform.localPosition = value;
                        return true;
                    }
                    else {
                        return false;
                    }
                default:
                    if (args.Self.transform.position != value) {
                        args.Self.transform.position = value;
                        return true;
                    }
                    else {
                        return false;
                    }
            }
        }
    }

    [System.Serializable]
    [CategoryMenu("Target", -99)]
    public class TargetPositionValue : Vector3Value {
        [SerializeField] private Space space;

        public override Vector3 Get(Args args) {
            switch (space) {
                case Space.Self:
                    return args.Target.transform.localPosition;
                default:
                    return args.Target.transform.position;
            }
        }

        public override bool Set(Vector3 value, Args args) {
            switch (space) {
                case Space.Self:
                    if (args.Target.transform.localPosition != value) {
                        args.Target.transform.localPosition = value;
                        return true;
                    }
                    else {
                        return false;
                    }
                default:
                    if (args.Target.transform.position != value) {
                        args.Target.transform.position = value;
                        return true;
                    }
                    else {
                        return false;
                    }
            }
        }
    }


    [System.Serializable]
    [CategoryMenu("Constant/Zero", -98)]
    public class ZeroVector3Value : Vector3Value {

        public override Vector3 Get(Args args) {
            return Vector3.zero;
        }

        public override bool Set(Vector3 value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Constant/One", -98)]
    public class OneVector3Value : Vector3Value {

        public override Vector3 Get(Args args) {
            return Vector3.one;
        }

        public override bool Set(Vector3 value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Constant/Forward", -98)]
    public class ForwardVector3Value : Vector3Value {

        public override Vector3 Get(Args args) {
            return Vector3.forward;
        }

        public override bool Set(Vector3 value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Constant/Back", -98)]
    public class BackVector3Value : Vector3Value {

        public override Vector3 Get(Args args) {
            return Vector3.back;
        }

        public override bool Set(Vector3 value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Constant/Up", -98)]
    public class UpVector3Value : Vector3Value {

        public override Vector3 Get(Args args) {
            return Vector3.up;
        }

        public override bool Set(Vector3 value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Constant/Down", -98)]
    public class DownVector3Value : Vector3Value {

        public override Vector3 Get(Args args) {
            return Vector3.down;
        }

        public override bool Set(Vector3 value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Constant/Left", -98)]
    public class LeftVector3Value : Vector3Value {

        public override Vector3 Get(Args args) {
            return Vector3.left;
        }

        public override bool Set(Vector3 value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Constant/Right", -98)]
    public class RightVector3Value : Vector3Value {

        public override Vector3 Get(Args args) {
            return Vector3.right;
        }

        public override bool Set(Vector3 value, Args args) {
            return false;
        }
    }


    [System.Serializable]
    [CategoryMenu("Random/Inside Unit Sphere")]
    public class RandomInsideUnitSphereValue : Vector3Value {
        public override Vector3 Get(Args args) {
            return Random.insideUnitSphere;
        }

        public override bool Set(Vector3 value, Args args) {
            return false;
        }
    }
}