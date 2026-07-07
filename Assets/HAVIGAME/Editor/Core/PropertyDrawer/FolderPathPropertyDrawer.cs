using UnityEditor;
using UnityEngine;

namespace HAVIGAME.Editor {
    [CustomPropertyDrawer(typeof(FolderPathAttribute))]
    public class FolderPathPropertyDrawer : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

            bool isString = property.propertyType == SerializedPropertyType.String;

            if (isString) {
                Rect textFieldRect = new Rect(position.position, position.size - new Vector2(55, 0));
                Rect buttonFieldRect = new Rect(position.position + new Vector2(position.width - 55, 0), new Vector2(55, position.height));

                property.stringValue = EditorGUI.TextField(textFieldRect, label, property.stringValue);

                if (GUI.Button(buttonFieldRect, "Browse")) {
                    string folderPath = UnityEditor.EditorUtility.OpenFolderPanel("Select Folder", string.IsNullOrEmpty(property.stringValue) ? Application.dataPath : property.stringValue, "");

                    if (property.stringValue != folderPath) {
                        property.stringValue = folderPath;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                    GUIUtility.ExitGUI();
                }
            } else {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
}
