using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HAVIGAME.Editor {
    [CustomPropertyDrawer(typeof(ConditionAttribute))]
    public class ConditionPropertyDrawer : PropertyDrawer {
        private bool isShowing, isDisable;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            if (isShowing || isDisable) {
                return EditorGUI.GetPropertyHeight(property, label, true);
            } else {
                return 0;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            ConditionAttribute conditionAttribute = attribute as ConditionAttribute;

            object obj = GetParent(property);

            object value = GetValue(obj, conditionAttribute.CompareName);

            isShowing = conditionAttribute.CompareValue == null ? value == null : conditionAttribute.CompareValue.Equals(value);

            if (isShowing) {
                EditorGUI.PropertyField(position, property, label, true);
            } else {
                isDisable = property.propertyPath.Contains(".Array.data");
                if (isDisable) {
                    GUI.enabled = false;
                    EditorGUI.PropertyField(position, property, label, true);
                    GUI.enabled = true;
                }
            }
        }

        private object GetParent(SerializedProperty property) {
            var path = property.propertyPath.Replace(".Array.data[", "[");
            object obj = property.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements.Take(elements.Length - 1)) {
                if (element.Contains("[")) {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue(obj, elementName, index);
                } else {
                    obj = GetValue(obj, element);
                }
            }
            return obj;
        }

        private object GetValue(object source, string name) {
            if (source == null)
                return null;
            Type originType = source.GetType();
            Type type = originType;
            FieldInfo field = null;

            while (type != null) {
                field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (field != null) break;

                type = type.BaseType;
            }

            if (field != null) {
                return field.GetValue(source);
            } else {
                type = originType;
                PropertyInfo property = null;

                while (type != null) {
                    property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (property != null) break;

                    type = type.BaseType;
                }

                if (property != null) {
                    return property.GetValue(source);
                }
            }

            return null;
        }

        private object GetValue(object source, string name, int index) {
            var enumerable = GetValue(source, name) as IEnumerable;
            var enm = enumerable.GetEnumerator();
            while (index-- >= 0)
                enm.MoveNext();
            return enm.Current;
        }
    }
}
