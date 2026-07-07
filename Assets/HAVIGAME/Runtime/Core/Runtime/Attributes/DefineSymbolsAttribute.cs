using System;

namespace HAVIGAME {

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DefineSymbolsAttribute : Attribute {
        public string DefineSymbol { get; }
        public string[] DependencyDefineSymbols { get; }

        public DefineSymbolsAttribute(string defineSymbol, params string[] dependencyDefineSymbols) {
            DefineSymbol = defineSymbol;
            DependencyDefineSymbols = dependencyDefineSymbols;
        }
    }
}