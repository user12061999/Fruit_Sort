using System;
using UnityEngine;

namespace HAVIGAME {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class IndentAttribute : PropertyAttribute {
        public int Indent { get; }
        public IndentMode Mode { get; }

        public IndentAttribute(int indent = 1, IndentMode mode = IndentMode.Local) {
            Indent = indent;
            Mode = mode;
        }

        public enum IndentMode {
            Local,
            World,
        }
    }
}