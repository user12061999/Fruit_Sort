using UnityEditor;
using UnityEngine;

namespace HAVIGAME.Editor {
    [CustomPropertyDrawer(typeof(SpriteFieldAttribute))]
    public class SpriteFieldPropertyDrawner : PropertyDrawer {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            SpriteFieldAttribute spriteFieldAttribute = (SpriteFieldAttribute)attribute;

            if (spriteFieldAttribute.size > 0) return spriteFieldAttribute.size;

            return spriteFieldAttribute.size;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginChangeCheck();

            Object spriteField = property.objectReferenceValue;

            spriteField = EditorGUI.ObjectField(position, label, spriteField, typeof(Sprite), false);

            if (EditorGUI.EndChangeCheck()) {
                property.objectReferenceValue = spriteField;
            }
        }
    }
}
