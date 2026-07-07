using System;
using UnityEngine;

namespace HAVIGAME {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class MinFieldAttribute : PropertyAttribute {
        public readonly float minValue;
        public readonly float snapValue;

        public MinFieldAttribute(float minValue, float snapValue = 0f) {
            this.minValue = minValue;
            this.snapValue = snapValue;
        }

        public MinFieldAttribute(int minValue, int snapValue = 0) {
            this.minValue = minValue;
            this.snapValue = snapValue;
        }
    }
}