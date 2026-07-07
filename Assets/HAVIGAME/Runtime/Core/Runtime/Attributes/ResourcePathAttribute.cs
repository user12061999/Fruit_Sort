using System;
using UnityEngine;

namespace HAVIGAME {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ResourcePathAttribute : PropertyAttribute {
        public readonly Type type;
        public readonly int height;

        public ResourcePathAttribute(Type type, int height = -1) {
            this.type = type;
            this.height = height;
        }
    }
}