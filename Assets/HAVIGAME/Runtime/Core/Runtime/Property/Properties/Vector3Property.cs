using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public sealed class Vector3Property : Property<Vector3Value, Vector3> {
        public static Vector3Property Create() {
            return new Vector3Property(new DefaultVector3Value());
        }

        public Vector3Property(Vector3Value value) : base(value) {

        }
    }

    [System.Serializable]
    public sealed class Vector3PropertyReadonly : PropertyReadonly<Vector3Value, Vector3> {
        public static Vector3PropertyReadonly Create() {
            return new Vector3PropertyReadonly(new DefaultVector3Value());
        }

        public Vector3PropertyReadonly(Vector3Value value) : base(value) {

        }
    }
}