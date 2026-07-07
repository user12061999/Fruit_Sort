using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public sealed class QuaternionProperty : Property<QuaternionValue, Quaternion> {
        public static QuaternionProperty Create() {
            return new QuaternionProperty(new DefaultQuaternionValue());
        }

        public QuaternionProperty(QuaternionValue value) : base(value) {

        }
    }

    [System.Serializable]
    public sealed class QuaternionPropertyReadonly : PropertyReadonly<QuaternionValue, Quaternion> {
        public static QuaternionPropertyReadonly Create() {
            return new QuaternionPropertyReadonly(new DefaultQuaternionValue());
        }

        public QuaternionPropertyReadonly(QuaternionValue value) : base(value) {

        }
    }
}