using UnityEngine;
using UnityEditor;

namespace HAVIGAME.Editor {
    [CustomPropertyDrawer(typeof(MaxFieldAttribute))]
    public class MaxFieldPropertyDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

            EditorGUI.BeginChangeCheck();

            EditorGUI.PropertyField(position, property, label);

            if (EditorGUI.EndChangeCheck()) {

                MaxFieldAttribute maxFieldAttribute = (MaxFieldAttribute)attribute;
                bool isFloat = property.propertyType == SerializedPropertyType.Float;
                bool isInteger = property.propertyType == SerializedPropertyType.Integer;

                if (isFloat || isInteger) {

                    float floatValue = 0;
                    int integerValue = 0;
                    if (isFloat) {
                        floatValue = property.floatValue;
                    } else if (isInteger) {
                        integerValue = property.intValue;
                    }

                    if (maxFieldAttribute.snapValue > 0f) {
                        if (isFloat) {
                            floatValue = Mathf.Round(floatValue / maxFieldAttribute.snapValue) * maxFieldAttribute.snapValue;
                        } else if (isInteger) {
                            integerValue = (int)(Mathf.Round(integerValue / maxFieldAttribute.snapValue) * maxFieldAttribute.snapValue);
                        }
                    }
                    if (isInteger) {
                        property.intValue = Mathf.Clamp(integerValue, int.MinValue, (int)maxFieldAttribute.maxValue);
                    } else {
                        property.floatValue = Mathf.Clamp(floatValue, float.MinValue, maxFieldAttribute.maxValue);
                    }
                }
            }
        }
    }
}