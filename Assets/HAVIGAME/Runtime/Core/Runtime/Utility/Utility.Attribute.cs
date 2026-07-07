using System;

namespace HAVIGAME {
    public static partial class Utility {
        public static class Attribute {
            public static T GetCustomAttribute<T>(Type type) where T : System.Attribute {
                return System.Attribute.GetCustomAttribute(type, typeof(T)) as T;
            }
        }
    }
}
