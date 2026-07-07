using System;
using UnityEngine;

namespace HAVIGAME {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class AssetPathAttribute : PropertyAttribute {
        public readonly Type type;
        public readonly int height;

        public AssetPathAttribute(Type type, int height = -1) {
            this.type = type;
            this.height = height;
        }
    }
}