using System;
using UnityEngine;

namespace HAVIGAME {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class InlineAttribute : PropertyAttribute {

        public InlineAttribute() {

        }
    }
}