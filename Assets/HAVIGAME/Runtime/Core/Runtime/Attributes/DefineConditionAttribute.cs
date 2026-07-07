using System;
using UnityEngine;

namespace HAVIGAME {

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DefineConditionAttribute : PropertyAttribute {
        public string[] DefineSymbols { get; }

        public DefineConditionAttribute(params string[] defineSymbols) {
            DefineSymbols = defineSymbols;
        }
    }
}