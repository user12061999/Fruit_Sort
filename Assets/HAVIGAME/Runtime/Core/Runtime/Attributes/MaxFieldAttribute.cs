using System;
using UnityEngine;

namespace HAVIGAME {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class MaxFieldAttribute : PropertyAttribute {
        public readonly float maxValue;
        public readonly float snapValue;

        public MaxFieldAttribute(float maxValue, float snapValue = 0f) {
            this.maxValue = maxValue;
            this.snapValue = snapValue;
        }

        public MaxFieldAttribute(int maxValue, int snapValue = 0) {
            this.maxValue = maxValue;
            this.snapValue = snapValue;
        }
    }
}