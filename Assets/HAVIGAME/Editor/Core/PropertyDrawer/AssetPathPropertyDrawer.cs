using UnityEditor;
using UnityEngine;

namespace HAVIGAME.Editor {
    [CustomPropertyDrawer(typeof(AssetPathAttribute))]
    public class AssetPathPropertyDrawer : PropertyDrawer {

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            AssetPathAttribute assetPathAttribute = attribute as AssetPathAttribute;
            
            if (assetPathAttribute.height > 0) return assetPathAttribute.height;

            return base.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            AssetPathAttribute assetPathAttribute = attribute as AssetPathAttribute;

            bool isString = property.propertyType == SerializedPropertyType.String;
            System.Type assetType = assetPathAttribute.type;
            bool isUnityObject = assetType.IsSubclassOf(typeof(Object));

            if (isString && isUnityObject) {
                string assetPath = property.stringValue;
                Object asset = AssetDatabase.LoadAssetAtPath(assetPath, assetType);

                EditorGUI.BeginChangeCheck();
                asset = EditorGUI.ObjectField(position, label, asset, assetType, false);
                if (EditorGUI.EndChangeCheck()) {
                    assetPath = AssetDatabase.GetAssetPath(asset);
                    property.stringValue = assetPath;
                }
            }
            else {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
}
