using System;
using UnityEngine;

namespace HAVIGAME {
    [AttributeUsage(AttributeTargets.Field)]
    public class ButtonFieldAttribute : PropertyAttribute {
        public readonly string name;
        public readonly string method;
        public readonly int width;

        public ButtonFieldAttribute(string name, string method, int width = 50) {
            this.name = name;
            this.method = method;
            this.width = width;
        }
    }
}
