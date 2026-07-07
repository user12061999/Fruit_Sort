using UnityEngine;
using UnityEditor;

namespace HAVIGAME.Editor {
    [CustomPropertyDrawer(typeof(DefineConditionAttribute))]
    public class DefineConditionPropertyDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

            DefineConditionAttribute defineCondition = attribute as DefineConditionAttribute;

            bool isBoolean = property.propertyType == SerializedPropertyType.Boolean;

            if (isBoolean) {
                bool hasDefineSymbols = EditorUtility.DefineSymbols.HasDefineSymbols(defineCondition.DefineSymbols);
                property.boolValue = hasDefineSymbols;

                GUI.Label(position, label);

                Rect btnRect = position;
                btnRect.x = btnRect.x + btnRect.width - 64;
                btnRect.width = 64;

                Color colorCached = GUI.color;
                GUI.color = hasDefineSymbols ? Color.green : Color.red;
                if (GUI.Button(btnRect, hasDefineSymbols ? "Enabled" : "Disabled")) {
                    if (hasDefineSymbols) {
                        EditorUtility.DefineSymbols.RemoveDefineSymbols(defineCondition.DefineSymbols);
                        property.boolValue = false;
                    }
                    else {
                        EditorUtility.DefineSymbols.AddDefineSymbols(defineCondition.DefineSymbols);
                        property.boolValue = true;
                    }
                }
                GUI.color = colorCached;
            }
            else {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
}