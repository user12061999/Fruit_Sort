using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace HAVIGAME.Editor {
    public class AdvancedTypeDropdown : AdvancedDropdown {

        public class AdvancedDropdownItemType : AdvancedDropdownItem {
            public Type Type { get; }
            public bool IsNull => Type == null;

            public AdvancedDropdownItemType(Type type, string name) : base(name) {
                Type = type;
            }
        }

        private const int minLine = 8;
        private const string nullDisplayName = "<null>";
        private static readonly char[] categorySplitChars = { '.', '/' };
        private static readonly AdvancedDropdownItemType nullItem = new AdvancedDropdownItemType(null, nullDisplayName);
        private static readonly Type unityObjectType = typeof(UnityEngine.Object);
        private static readonly Dictionary<Type, AdvancedDropdownItem> advancedDropdownItemTrees = new Dictionary<Type, AdvancedDropdownItem>();

        public Action<AdvancedDropdownItemType> onItemSelected;
        private Type type;

        public AdvancedTypeDropdown(Type type, AdvancedDropdownState state) : base(state) {
            this.type = type;
            minimumSize = new Vector2(minimumSize.x, EditorGUIUtility.singleLineHeight * (minLine + 2));
        }

        protected override AdvancedDropdownItem BuildRoot() {
            AdvancedDropdownItem root = GetAdvancedDropdownItemTree(type);
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item) {
            base.ItemSelected(item);

            if (item is AdvancedDropdownItemType itemType) {
                onItemSelected?.Invoke(itemType);
            }
        }

        private static AdvancedDropdownItem GetAdvancedDropdownItemTree(Type type) {
            if (advancedDropdownItemTrees.ContainsKey(type)) {
                AdvancedDropdownItem root = advancedDropdownItemTrees[type];
                return root;
            }
            else {
                AdvancedDropdownItem root = CreateAdvancedDropdownTree(type);
                advancedDropdownItemTrees[type] = root;
                return root;
            }
        }

        private static AdvancedDropdownItem CreateAdvancedDropdownTree(Type type) {
            AdvancedDropdownItem root = new AdvancedDropdownItem("Select Type");

            root.AddChild(nullItem);
            Type[] typesDerived = TypeCache.GetTypesDerivedFrom(type)
                .Append(type)
                .Where(typeDerived =>
                      (typeDerived.IsPublic || typeDerived.IsNestedPublic) &&
                      !typeDerived.IsAbstract &&
                      !typeDerived.IsGenericType &&
                      !unityObjectType.IsAssignableFrom(typeDerived) &&
                      Attribute.IsDefined(typeDerived, typeof(SerializableAttribute)))
                .ToArray();

            System.Array.Sort(typesDerived, CompareType);

            foreach (Type typeDerived in typesDerived) {
                CategoryMenuAttribute categoryAttribute = Utility.Attribute.GetCustomAttribute<CategoryMenuAttribute>(typeDerived);
                string fullName = null;
                string name = null;
                string[] paths = null;

                if (categoryAttribute != null) {
                    name = categoryAttribute.Name;
                    fullName = categoryAttribute.FullName;
                }
                else {
                    name = ObjectNames.NicifyVariableName(typeDerived.Name);
                    fullName = typeDerived.FullName;
                }

                string[] categories = fullName.Split(categorySplitChars);
                paths = new string[categories.Length - 1];
                for (int i = 0; i < categories.Length - 1; ++i) {
                    paths[i] = categories[i];
                }

                AdvancedDropdownItem parent = root;
                foreach (string pathName in paths) {
                    if (!string.IsNullOrEmpty(pathName)) {
                        AdvancedDropdownItem child = GetChild(parent, pathName);

                        if (child == null) {
                            child = new AdvancedDropdownItem(pathName);
                            parent.AddChild(child);
                        }
                        parent = child;
                    }
                }

                AdvancedDropdownItem item = GetChild(parent, name);
                if (item == null) {
                    item = new AdvancedDropdownItemType(typeDerived, name);
                    parent.AddChild(item);
                }
                else {
                    item.name = name;
                }
            }

            return root;
        }

        private static int CompareType(Type x, Type y) {
            CategoryMenuAttribute xCategoryAttribute = Utility.Attribute.GetCustomAttribute<CategoryMenuAttribute>(x);
            CategoryMenuAttribute yCategoryAttribute = Utility.Attribute.GetCustomAttribute<CategoryMenuAttribute>(y);

            if (xCategoryAttribute != null && yCategoryAttribute != null) {
                int orderCompare = xCategoryAttribute.Order.CompareTo(yCategoryAttribute.Order);
                if (orderCompare != 0) {
                    return orderCompare;
                }
                else {
                    return xCategoryAttribute.Name.CompareTo(yCategoryAttribute.Name);
                }
            }
            else {
                if (x != null && y != null) {
                    return x.Name.CompareTo(y.Name);
                }
                else if (x == null && y == null) {
                    return 0;
                }
                else if (x == null) {
                    return -1;
                }
                else {
                    return 1;
                }
            }
        }

        private static AdvancedDropdownItem GetChild(AdvancedDropdownItem target, string name) {
            foreach (var item in target.children) {
                if (item.name.Equals(name)) return item;
            }
            return null;
        }
    }
}