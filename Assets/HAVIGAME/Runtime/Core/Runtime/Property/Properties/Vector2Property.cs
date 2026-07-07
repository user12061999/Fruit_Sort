using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public sealed class Vector2Property : Property<Vector2Value, Vector2> {
        public static Vector2Property Create() {
            return new Vector2Property(new DefaultVector2Value());
        }

        public Vector2Property(Vector2Value value) : base(value) {

        }
    }

    [System.Serializable]
    public sealed class Vector2PropertyReadonly : PropertyReadonly<Vector2Value, Vector2> {
        public static Vector2PropertyReadonly Create() {
            return new Vector2PropertyReadonly(new DefaultVector2Value());
        }

        public Vector2PropertyReadonly(Vector2Value value) : base(value) {

        }
    }
}