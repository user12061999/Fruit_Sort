using System;
using UnityEngine;

namespace HAVIGAME {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class DataFieldAttribute : PropertyAttribute {
        public readonly string name;
        public readonly string onConvertCallback;

        public DataFieldAttribute(string name, string onConvertCallback = null) {
            this.name = name;
            this.onConvertCallback = onConvertCallback;
        }
    }
}