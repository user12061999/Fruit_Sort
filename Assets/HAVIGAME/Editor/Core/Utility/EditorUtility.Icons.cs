using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HAVIGAME.Editor {
    public static partial class EditorUtility {

        public static class Icons {
            public class IconHolder {

                private string path;
                private Texture2D icon;

                public Texture2D Icon {
                    get {
                        if (icon != null) {
                            return icon;
                        }

                        icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/HAVIGAME/Editor/{path}");

                        return icon;
                    }
                }

                public IconHolder(string path) {
                    this.path = path;
                }
            }

            private static readonly Dictionary<string, IconHolder> iconHolders = new Dictionary<string, IconHolder>(16);

            public static Texture2D GetIcon(string path) {
                if (!string.IsNullOrEmpty(path)) {
                    if (iconHolders.TryGetValue(path, out IconHolder result)) {
                        return result.Icon;
                    }
                    else {
                        IconHolder iconHolder = new IconHolder(path);
                        iconHolders[path] = iconHolder;
                        return iconHolder.Icon;
                    }
                }
                return null;
            }
        }

        public static class IconMiner {
            [MenuItem("Window/HAVIGAME/Export Unity Icons", priority = -1001)]
            private static void ExportEditorIcons() {
                UnityEditor.EditorUtility.DisplayProgressBar("Export editor icons", "Exporting...", 0.0f);
                try {
                    AssetBundle editorAssetBundle = GetEditorAssetBundle();
                    string iconsPath = GetIconsPath();
                    string folderPath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - 6), "Exported Icons");
                    int count = 0;
                    foreach (var assetName in EnumerateIcons(editorAssetBundle, iconsPath)) {

                        Texture2D icon = editorAssetBundle.LoadAsset<Texture2D>(assetName);

                        if (icon == null) continue;

                        SaveTexture(icon, folderPath, icon.name);

                        count++;
                    }

                    Debug.Log($"{count} icons has been exported!");
                }
                finally {
                    UnityEditor.EditorUtility.ClearProgressBar();
                }
            }

            [MenuItem("Window/HAVIGAME/Export Preview Icons", priority = -1002)]
            private static void ExportPreviewIcons() {
                UnityEditor.EditorUtility.DisplayProgressBar("Export preview icons", "Exporting...", 0.0f);
                try {
                    int count = 0;
                    string folderPath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - 6), "Exported Icons");

                    foreach (var assetGUID in Selection.assetGUIDs) {
                        string assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);

                        UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                        if (asset == null)
                            continue;

                        UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(asset);

                        Texture2D icon = editor.RenderStaticPreview(assetPath, new UnityEngine.Object[] { asset }, 2048, 2048);

                        UnityEditor.Editor.DestroyImmediate(editor);

                        if (icon == null) continue;

                        SaveTexture(icon, folderPath, asset.name);
                        count++;
                    }

                    Debug.Log($"{count} icons has been exported!");
                } finally {
                    UnityEditor.EditorUtility.ClearProgressBar();
                }
            }

            private static void SaveTexture(Texture2D texture, string folder, string name) {
                Texture2D readableTexture = new Texture2D(texture.width, texture.height, texture.format, texture.mipmapCount > 1);

                UnityEngine.Graphics.CopyTexture(texture, readableTexture);

                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string path = Path.Combine(folder, name + ".png");

                File.WriteAllBytes(path, readableTexture.EncodeToPNG());
            }

            private static IEnumerable<string> EnumerateIcons(AssetBundle editorAssetBundle, string iconsPath) {
                foreach (var assetName in editorAssetBundle.GetAllAssetNames()) {
                    if (assetName.StartsWith(iconsPath, StringComparison.OrdinalIgnoreCase) == false)
                        continue;
                    if (assetName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) == false &&
                        assetName.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) == false)
                        continue;

                    yield return assetName;
                }
            }

            private static AssetBundle GetEditorAssetBundle() {
                var editorGUIUtility = typeof(EditorGUIUtility);
                var getEditorAssetBundle = editorGUIUtility.GetMethod(
                    "GetEditorAssetBundle",
                    BindingFlags.NonPublic | BindingFlags.Static);

                return (AssetBundle)getEditorAssetBundle.Invoke(null, new object[] { });
            }

            private static string GetIconsPath() {
#if UNITY_2018_3_OR_NEWER
                return UnityEditor.Experimental.EditorResources.iconsPath;
#else
            var assembly = typeof(EditorGUIUtility).Assembly;
            var editorResourcesUtility = assembly.GetType("UnityEditorInternal.EditorResourcesUtility");

            var iconsPathProperty = editorResourcesUtility.GetProperty(
                "iconsPath",
                BindingFlags.Static | BindingFlags.Public);

            return (string)iconsPathProperty.GetValue(null, new object[] { });
#endif
            }
        }
    }
}


