using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace HAVIGAME.Editor {
    public class SettingsTreeViewItem : TreeViewItem {
        public string Description { get; }
        public string Document { get; }
        public UnityEngine.Object SettingsAsset { get; }

        public SettingsTreeViewItem(GUIContent content, string description, string document, UnityEngine.Object settingsAsset) : base(content) {
            SettingsAsset = settingsAsset;
            Description = description;
            Document = document;
        }
    }

    public class SettingsTreeView : TreeView {
        private static readonly char categorySplitChar = '/';

        public Action<SettingsTreeViewItem> onSelected;

        protected override TreeViewItem BuildRoot() {
            TreeViewItem root = new TreeViewItem(new GUIContent("Root"));

            Type[] typesDerived = TypeCache.GetTypesWithAttribute<SettingMenuAttribute>()
                .Where(typeDerived =>
                      (typeDerived.IsPublic || typeDerived.IsNestedPublic) &&
                      !typeDerived.IsAbstract &&
                      !typeDerived.IsGenericType)
                .ToArray();

            System.Array.Sort(typesDerived, CompareType);

            foreach (Type typeDerived in typesDerived) {
                SettingMenuAttribute settingMenuAttribute = Utility.Attribute.GetCustomAttribute<SettingMenuAttribute>(typeDerived);
                string fullName = settingMenuAttribute.FullName;
                string name = settingMenuAttribute.Name;
                string[] paths = null;

                string[] categories = fullName.Split(categorySplitChar);
                paths = new string[categories.Length - 1];
                for (int i = 0; i < categories.Length - 1; ++i) {
                    paths[i] = categories[i];
                }

                TreeViewItem parent = root;
                foreach (string pathName in paths) {
                    if (!string.IsNullOrEmpty(pathName)) {
                        TreeViewItem child = GetChild(parent, pathName);

                        if (child == null) {
                            child = new TreeViewItem(new GUIContent(pathName));
                            parent.AddChild(child);
                        }
                        parent = child;
                    }
                }

                TreeViewItem item = GetChild(parent, name);
                if (item == null) {
                    item = new SettingsTreeViewItem(new GUIContent(name, EditorUtility.Icons.GetIcon(settingMenuAttribute.Icon)), settingMenuAttribute.Description, settingMenuAttribute.Document, GetSettingsAsset(settingMenuAttribute.Type));
                    parent.AddChild(item);
                } else {
                    item.content = new GUIContent(name, EditorUtility.Icons.GetIcon(settingMenuAttribute.Icon));
                }
            }

            return root;
        }

        protected override void OnItemSelected(TreeViewItem item) {
            if (selectedItem != item) {
                base.OnItemSelected(item);

                if (item is SettingsTreeViewItem settingsViewItem) {
                    onSelected?.Invoke(settingsViewItem);
                } else {
                    onSelected?.Invoke(null);
                }
            }
        }

        private static TreeViewItem GetChild(TreeViewItem target, string name) {
            foreach (var item in target.GetChilds) {
                if (item.content.text.Equals(name)) return item;
            }
            return null;
        }

        private static int CompareType(Type x, Type y) {
            SettingMenuAttribute xSettingsMenuAttribute = Utility.Attribute.GetCustomAttribute<SettingMenuAttribute>(x);
            SettingMenuAttribute ySettingsMenuAttribute = Utility.Attribute.GetCustomAttribute<SettingMenuAttribute>(y);

            if (xSettingsMenuAttribute != null && ySettingsMenuAttribute != null) {
                int orderCompare = xSettingsMenuAttribute.Order.CompareTo(ySettingsMenuAttribute.Order);
                if (orderCompare != 0) {
                    return orderCompare;
                } else {
                    return xSettingsMenuAttribute.Name.CompareTo(ySettingsMenuAttribute.Name);
                }
            } else {
                if (x != null && y != null) {
                    return x.Name.CompareTo(y.Name);
                } else if (x == null && y == null) {
                    return 0;
                } else if (x == null) {
                    return -1;
                } else {
                    return 1;
                }
            }
        }

        private static UnityEngine.Object GetSettingsAsset(Type type) {
            string path = $"Settings/{type.Name}";
            UnityEngine.Object settingsAsset = Resources.Load<UnityEngine.Object>(path);

            if (settingsAsset != null) {
                return settingsAsset;
            } else {
                return CreateSettingsAsset(type);
            }
        }

        private static UnityEngine.Object CreateSettingsAsset(Type type) {
            if (!type.IsAbstract && type.IsSubclassOf(typeof(UnityEngine.Object))) {

                ScriptableObject settingsAsset = ScriptableObject.CreateInstance(type);
                settingsAsset.name = type.Name;
                string directory = Path.Combine(Application.dataPath, "Resources/Settings");
                if (!Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }
                AssetDatabase.CreateAsset(settingsAsset, $"Assets/Resources/Settings/{settingsAsset.name}.asset");
                return settingsAsset;
            } else {
                return null;
            }
        }
    }
}
