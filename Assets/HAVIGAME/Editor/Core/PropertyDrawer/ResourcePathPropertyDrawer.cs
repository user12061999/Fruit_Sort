using UnityEditor;
using UnityEngine;

namespace HAVIGAME.Editor {
    [CustomPropertyDrawer(typeof(ResourcePathAttribute))]
    public class ResourcePathPropertyDrawer : PropertyDrawer {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            ResourcePathAttribute resourcePathAttribute = attribute as ResourcePathAttribute;

            if (resourcePathAttribute.height > 0) return resourcePathAttribute.height;

            return base.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            ResourcePathAttribute resourcePathAttribute = attribute as ResourcePathAttribute;

            bool isString = property.propertyType == SerializedPropertyType.String;
            System.Type assetType = resourcePathAttribute.type;
            bool isUnityObject = assetType.IsSubclassOf(typeof(Object));

            if (isString && isUnityObject) {
                string resourcePath = property.stringValue;
                Object asset = Resources.Load(resourcePath, assetType);

                EditorGUI.BeginChangeCheck();
                asset = EditorGUI.ObjectField(position, label, asset, assetType, false);
                if (EditorGUI.EndChangeCheck()) {
                    string assetPath = AssetDatabase.GetAssetPath(asset);
                    string fullPath = System.IO.Path.Combine(Application.dataPath, assetPath);
                    string extension = System.IO.Path.GetExtension(fullPath);
                    int startIndex = assetPath.IndexOf(@"/Resources/", 0);
                    string resourcePathWithExtension = assetPath.Substring(startIndex + 11);
                    resourcePath = resourcePathWithExtension.Substring(0, resourcePathWithExtension.Length - extension.Length);
                    property.stringValue = resourcePath;
                }
            }
            else {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
}
