using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HAVIGAME.Editor {

    public class SettingsWindow : EditorWindow {

        [MenuItem("Window/HAVIGAME/Settings", priority = -1)]
        public static void Create() {
            SettingsWindow window = GetWindow<SettingsWindow>();
            window.minSize = new Vector2(600, 300);
        }

        private EditorUtility.HorizontalSplitView horizontalSplitView = new EditorUtility.HorizontalSplitView();
        private Vector2 itemSettingScroll;
        private UnityEditor.Editor itemSettingEditor = null;
        private SettingsTreeView treeView;
        private SettingsTreeViewItem itemView;
        private string defineSymbol;
        private string[] dependencyDefineSymbols;


        private void OnEnable() {
            Reload();

            titleContent = new GUIContent("Settings", EditorUtility.Icons.GetIcon("icon_settings"));
        }

        private void Reload() {
            treeView = new SettingsTreeView();
            treeView.onSelected = OnTreeViewItemSelected;
            treeView.Build();
        }

        private void OnGUI() {
            horizontalSplitView.Begin();
            DrawTreeView();
            horizontalSplitView.Split();

            GUILayout.Space(4);

            GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawItemHeader();
            itemSettingScroll = GUILayout.BeginScrollView(itemSettingScroll);
            DrawItemSettings();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            if (horizontalSplitView.End()) {
                Repaint();
            }
        }

        private void DrawTreeView() {
            treeView.OnGUI();
        }

        private void OnTreeViewItemSelected(SettingsTreeViewItem item) {
            itemView = item;

            if (itemSettingEditor != null) {
                DestroyImmediate(itemSettingEditor);
                itemSettingEditor = null;
            }

            if (itemView != null && itemView.SettingsAsset != null) {
                itemSettingEditor = UnityEditor.Editor.CreateEditor(item.SettingsAsset);

                DefineSymbolsAttribute defineSymbolsAttribute = Utility.Attribute.GetCustomAttribute<DefineSymbolsAttribute>(item.SettingsAsset.GetType());

                if (defineSymbolsAttribute != null) {
                    defineSymbol = defineSymbolsAttribute.DefineSymbol;
                    dependencyDefineSymbols = defineSymbolsAttribute.DependencyDefineSymbols;
                }
                else {
                    defineSymbol = null;
                    dependencyDefineSymbols = null;
                }
            }
            else {
                defineSymbol = null;
                dependencyDefineSymbols = null;
            }
        }

        private void DrawItemSettings() {
            if (itemSettingEditor != null) {
                if (IsItemEnabled()) {
                    itemSettingEditor.OnInspectorGUI();
                }
            }
        }

        private void DrawItemHeader() {
            if (itemSettingEditor != null) {
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();

                GUILayout.Label(itemView.content, EditorUtility.Styles.WhiteLargeLabel, GUILayout.Height(32));

                if (!string.IsNullOrEmpty(itemView.Document)) {
                    if (GUILayout.Button("Document", EditorUtility.Styles.Toolbarbutton, GUILayout.Width(70))) {
                        Application.OpenURL(itemView.Document);
                    }
                }

                if (GUILayout.Button("Select Asset", EditorUtility.Styles.Toolbarbutton, GUILayout.Width(80))) {
                    Selection.activeObject = itemView.SettingsAsset;
                    EditorGUIUtility.PingObject(itemView.SettingsAsset);
                }

                if (!string.IsNullOrEmpty(defineSymbol) || dependencyDefineSymbols != null) {
                    bool isEnabled = IsItemEnabled();

                    if (GUILayout.Button(isEnabled ? "Enabled" : "<color=red>Disabled</color>", EditorUtility.Styles.ToolbarbuttonRichText, GUILayout.Width(60))) {
                        if (isEnabled) {
                            EditorUtility.DefineSymbols.RemoveDefineSymbols(defineSymbol);
                        }
                        else {
                            EditorUtility.DefineSymbols.AddDefineSymbols(defineSymbol);
                        }
                    }
                }

                GUILayout.EndHorizontal();

                GUILayout.EndVertical();

                EditorGUILayout.HelpBox(itemView.Description, MessageType.Info);

                GUILayout.Button("", GUILayout.Height(3));
            }
        }

        private IEnumerable<string> GetAssetPath(UnityEngine.Object assetObject) {
            yield return AssetDatabase.GetAssetPath(assetObject);
        }

        private bool IsItemEnabled() {
            bool isLocalEnabled = true;
            bool isDependencyEnabled = true;

            if (!string.IsNullOrEmpty(defineSymbol)) isLocalEnabled = EditorUtility.DefineSymbols.HasDefineSymbols(defineSymbol);
            if (dependencyDefineSymbols != null) isDependencyEnabled = EditorUtility.DefineSymbols.HasDefineSymbols(dependencyDefineSymbols);
            bool isEnabled = isLocalEnabled && isDependencyEnabled;
            return isEnabled;
        }
    }
}
