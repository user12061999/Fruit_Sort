using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace HAVIGAME.Editor {
    [CustomPropertyDrawer(typeof(ButtonFieldAttribute))]
    public class ButtonFieldPropertyDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

            ButtonFieldAttribute button = attribute as ButtonFieldAttribute;

            Rect buttonRect = new Rect(position.x + (position.width - button.width), position.y, button.width, position.height);
            position.width -= button.width;

            EditorGUI.PropertyField(position, property, label);

            if (GUI.Button(buttonRect, button.name)) {
                System.Type objectType = property.serializedObject.targetObject.GetType();
                string methodName = button.method;

                MethodInfo methodInfo = objectType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (methodInfo != null)
                    methodInfo.Invoke(property.serializedObject.targetObject, null);
                else {
                    Debug.LogWarning($"Unable to find method {methodName} in {objectType}");
                }
            }
        }
    }
}