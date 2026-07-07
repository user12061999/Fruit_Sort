using UnityEditor;
using UnityEngine;

namespace HAVIGAME.Editor {
    [CustomPropertyDrawer(typeof(FilePathAttribute))]
    public class FilePathPropertyDrawer : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            FilePathAttribute filePathAttribute = attribute as FilePathAttribute;

            bool isString = property.propertyType == SerializedPropertyType.String;

            if (isString) {
                Rect textFieldRect = new Rect(position.position, position.size - new Vector2(55, 0));
                Rect buttonFieldRect = new Rect(position.position + new Vector2(position.width - 55, 0), new Vector2(55, position.height));

                property.stringValue = EditorGUI.TextField(textFieldRect, label, property.stringValue);

                if (GUI.Button(buttonFieldRect, "Browse")) {
                    string filePath = UnityEditor.EditorUtility.OpenFilePanel("Select Folder", Application.dataPath, filePathAttribute.extension);

                    if (property.stringValue != filePath) {
                        property.stringValue = filePath;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                    GUIUtility.ExitGUI();
                }
            }
            else {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
}
