using UnityEditor;
using UnityEngine;

namespace HAVIGAME.Editor {
    [CustomPropertyDrawer(typeof(TextureFieldAttribute))]
    public class TextureFieldPropertyDrawer : PropertyDrawer {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            TextureFieldAttribute textureFieldAttribute = (TextureFieldAttribute)attribute;

            if (textureFieldAttribute.size > 0) return textureFieldAttribute.size;

            return textureFieldAttribute.size;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginChangeCheck();

            Object textureField = property.objectReferenceValue;

            textureField = EditorGUI.ObjectField(position, label, textureField, typeof(Texture2D), false);

            if (EditorGUI.EndChangeCheck()) {
                property.objectReferenceValue = textureField;
            }
        }
    }
}