using UnityEditor;
using UnityEngine;

namespace HAVIGAME.Editor {
    [CustomPropertyDrawer(typeof(IndentAttribute))]
    public class IndentPropertyDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            IndentAttribute indentAttribute = (IndentAttribute)attribute;

            EditorGUI.indentLevel += indentAttribute.Indent;
            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.indentLevel -= indentAttribute.Indent;
        }
    }
}