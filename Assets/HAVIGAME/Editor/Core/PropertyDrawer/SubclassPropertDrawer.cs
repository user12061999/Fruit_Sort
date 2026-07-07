using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEditor.IMGUI.Controls;

namespace HAVIGAME.Editor {

    [CustomPropertyDrawer(typeof(SubclassAttribute), true)]
    public class SubclassPropertDrawer : PropertyDrawer {
        private static readonly GUIContent nullDisplayName = new GUIContent("<null>");
        private static readonly GUIContent usNotManagedReferenceLabel = new GUIContent("The property type is not managed reference.");
        private static readonly Dictionary<string, Type> typeNames = new Dictionary<string, Type>();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUI.GetPropertyHeight(property, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.ManagedReference) {
                Rect popupPosition = position;
                popupPosition.width -= EditorGUIUtility.labelWidth;
                popupPosition.x += EditorGUIUtility.labelWidth;
                popupPosition.height = EditorGUIUtility.singleLineHeight;

                string valueTypeName = property.managedReferenceFieldTypename;
                Type valueType = GetType(valueTypeName);

                if (EditorGUI.DropdownButton(popupPosition, GetTypeName(property), FocusType.Keyboard)) {
                    AdvancedTypeDropdown advancedTypeDropdown = new AdvancedTypeDropdown(valueType, new AdvancedDropdownState());
                    advancedTypeDropdown.onItemSelected =
                        (itemType) => {
                            object obj = (itemType.Type != null) ? Activator.CreateInstance(itemType.Type) : null;

                            property.managedReferenceValue = obj;
                            property.isExpanded = (obj != null);
                            property.serializedObject.ApplyModifiedProperties();
                            property.serializedObject.Update();

                        };
                    advancedTypeDropdown.Show(popupPosition);
                }

                EditorGUI.PropertyField(position, property, label, true);
            }
            else {
                EditorGUI.LabelField(position, label, usNotManagedReferenceLabel);
            }

            EditorGUI.EndProperty();
        }

        private IEnumerable<string> GetTargetObjectPath(UnityEngine.Object targetObject) {
            yield return AssetDatabase.GetAssetPath(targetObject);
        }

        private static GUIContent GetTypeName(SerializedProperty property) {
            string valueTypeName = property.managedReferenceFullTypename;

            if (string.IsNullOrEmpty(valueTypeName)) {
                return nullDisplayName;
            }

            Type type = GetType(valueTypeName);

            if (type == null) {
                return nullDisplayName;
            }

            CategoryMenuAttribute categoryAttribute = Utility.Attribute.GetCustomAttribute<CategoryMenuAttribute>(type);

            string name = null;

            if (categoryAttribute != null) {
                name = categoryAttribute.Name;
            }
            else {
                name = ObjectNames.NicifyVariableName(type.Name);
            }

            GUIContent result = new GUIContent(name);
            return result;
        }

        private static Type GetType(string typeName) {
            if (typeNames.ContainsKey(typeName)) {
                return typeNames[typeName];
            }
            else {
                int splitIndex = typeName.IndexOf(' ');
                var assembly = Assembly.Load(typeName.Substring(0, splitIndex));
                Type type = assembly.GetType(typeName.Substring(splitIndex + 1));
                typeNames[typeName] = type;
                return type;
            }
        }
    }
}