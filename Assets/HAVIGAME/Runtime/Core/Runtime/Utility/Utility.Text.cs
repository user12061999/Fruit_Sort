using System;
using System.Globalization;
using System.Text;

namespace HAVIGAME {
    public static partial class Utility {
        public static class Text {
            private const int stringBuilderCapacity = 256;
            private static readonly StringBuilder stringBuilder = new StringBuilder(stringBuilderCapacity);

            public static string Format<T>(string format, T arg) {
                if (format == null) {
                    throw new Exception("Format is invalid.");
                }

                stringBuilder.Length = 0;
                stringBuilder.AppendFormat(format, arg);
                return stringBuilder.ToString();
            }

            public static string Format<T1, T2>(string format, T1 arg1, T2 arg2) {
                if (format == null) {
                    throw new Exception("Format is invalid.");
                }

                stringBuilder.Length = 0;
                stringBuilder.AppendFormat(format, arg1, arg2);
                return stringBuilder.ToString();
            }

            public static string Format<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3) {
                if (format == null) {
                    throw new Exception("Format is invalid.");
                }

                stringBuilder.Length = 0;
                stringBuilder.AppendFormat(format, arg1, arg2, arg3);
                return stringBuilder.ToString();
            }
        }
    }
}
