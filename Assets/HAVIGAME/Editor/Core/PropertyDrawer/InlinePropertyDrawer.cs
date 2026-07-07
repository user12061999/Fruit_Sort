using UnityEditor;
using UnityEngine;

namespace HAVIGAME.Editor {
    [CustomPropertyDrawer(typeof(InlineAttribute))]
    public class InlinePropertyDrawer : PropertyDrawer {

        private UnityEditor.Editor inlineEditor;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            if (property.propertyType == SerializedPropertyType.ObjectReference) {
                if (property.objectReferenceValue != null) {

                    float totalHeight = EditorGUIUtility.singleLineHeight;

                    if (property.isExpanded) {
                        SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue);
                        SerializedProperty prop = serializedObject.GetIterator();
                        if (prop.NextVisible(true)) {
                            do {
                                var subProp = serializedObject.FindProperty(prop.name);
                                float height = EditorGUI.GetPropertyHeight(subProp, null, true) + EditorGUIUtility.standardVerticalSpacing;
                                totalHeight += height;
                            }
                            while (prop.NextVisible(false));
                        }
                        totalHeight += EditorGUIUtility.standardVerticalSpacing;
                        serializedObject.Dispose();
                    }
                    return totalHeight;
                }
                else {
                    return base.GetPropertyHeight(property, label);
                }
            }
            else {
                return base.GetPropertyHeight(property, label);
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (property.propertyType == SerializedPropertyType.ObjectReference) {
                EditorGUI.PropertyField(position, property, label, true);

                if (property.objectReferenceValue != null) {
                    UnityEditor.Editor.CreateCachedEditor(property.objectReferenceValue, null, ref inlineEditor);

                    EditorGUI.indentLevel++;
                    GUILayout.BeginVertical("Box");

                    inlineEditor.OnInspectorGUI();

                    GUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                }
                else {
                    if (inlineEditor != null) {
                        Object.DestroyImmediate(inlineEditor);
                        inlineEditor = null;
                    }
                }
            }
            else {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
}