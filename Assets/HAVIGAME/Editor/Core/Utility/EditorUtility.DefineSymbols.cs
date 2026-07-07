using System.Collections.Generic;
using UnityEditor;
using System.Linq;

namespace HAVIGAME.Editor {
    public static partial class EditorUtility {
        public static class DefineSymbols {

            public static bool HasDefineSymbol(string symbol) {
                List<string> defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(GetCurrentBuildTargetGroup()).Split(';').ToList();

                return defineSymbols.Contains(symbol);
            }

            public static bool HasDefineSymbols(params string[] symbols) {
                List<string> defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(GetCurrentBuildTargetGroup()).Split(';').ToList();

                foreach (var symbol in symbols) {
                    if (!defineSymbols.Contains(symbol)) {
                        return false;
                    }
                }
                return true;
            }

            public static void AddDefineSymbol(string symbol) {
                List<string> defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(GetCurrentBuildTargetGroup()).Split(';').ToList();

                if (!defineSymbols.Contains(symbol)) {
                    defineSymbols.Add(symbol);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(GetCurrentBuildTargetGroup(), defineSymbols.ToArray());
                }
            }

            public static void AddDefineSymbols(params string[] symbols) {
                List<string> defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(GetCurrentBuildTargetGroup()).Split(';').ToList();

                foreach (var symbol in symbols) {
                    if (!defineSymbols.Contains(symbol)) {
                        defineSymbols.Add(symbol);
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(GetCurrentBuildTargetGroup(), defineSymbols.ToArray());
                    }
                }
            }

            public static void RemoveDefineSymbol(string symbol) {
                List<string> defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(GetCurrentBuildTargetGroup()).Split(';').ToList();

                if (defineSymbols.Remove(symbol)) {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(GetCurrentBuildTargetGroup(), defineSymbols.ToArray());
                }
            }

            public static void RemoveDefineSymbols(params string[] symbols) {
                List<string> defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(GetCurrentBuildTargetGroup()).Split(';').ToList();

                foreach (var symbol in symbols) {
                    if (defineSymbols.Remove(symbol)) {
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(GetCurrentBuildTargetGroup(), defineSymbols.ToArray());
                    }
                }
            }

            public static BuildTargetGroup GetCurrentBuildTargetGroup() {
                switch (EditorUserBuildSettings.activeBuildTarget) {
                    case BuildTarget.iOS:
                        return BuildTargetGroup.iOS;
                    case BuildTarget.Android:
                        return BuildTargetGroup.Android;
                    default:
                        return BuildTargetGroup.Standalone;
                }
            }
        }
    }
}
