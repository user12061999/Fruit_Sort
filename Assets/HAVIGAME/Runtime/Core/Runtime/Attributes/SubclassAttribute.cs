using System;
using UnityEngine;

namespace HAVIGAME {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SubclassAttribute : PropertyAttribute {
        public SubclassAttribute() {

        }
    }
}