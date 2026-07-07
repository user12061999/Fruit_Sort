using UnityEngine;
using UnityEditor;

namespace HAVIGAME.Editor {
    [CustomPropertyDrawer(typeof(MinFieldAttribute))]
    public class MinFieldPropertyDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

            EditorGUI.BeginChangeCheck();

            EditorGUI.PropertyField(position, property, label);

            if (EditorGUI.EndChangeCheck()) {

                MinFieldAttribute minFieldAttribute = (MinFieldAttribute)attribute;
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

                    if (minFieldAttribute.snapValue > 0f) {
                        if (isFloat) {
                            floatValue = Mathf.Round(floatValue / minFieldAttribute.snapValue) * minFieldAttribute.snapValue;
                        } else if (isInteger) {
                            integerValue = (int)(Mathf.Round(integerValue / minFieldAttribute.snapValue) * minFieldAttribute.snapValue);
                        }
                    }
                    if (isInteger) {
                        property.intValue = Mathf.Clamp(integerValue, (int)minFieldAttribute.minValue, int.MaxValue);
                    } else {
                        property.floatValue = Mathf.Clamp(floatValue, minFieldAttribute.minValue, float.MaxValue);
                    }
                }
            }
        }
    }
}