using System;
using UnityEngine;

namespace HAVIGAME {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class FilePathAttribute : PropertyAttribute {
        public readonly string extension;

        public FilePathAttribute(string extension) {
            this.extension = extension.Replace(".", "");
        }
    }
}