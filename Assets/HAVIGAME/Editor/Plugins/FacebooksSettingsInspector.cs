#if FACEBOOK

using UnityEngine;
using UnityEditor;

namespace HAVIGAME.Plugins.Facebook.Editor {
    [CustomEditor(typeof(FacebooksSettings))]
    public class FacebooksSettingsInspector : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            GUILayout.Space(5);

            EditorGUILayout.HelpBox("IMPORTANT: Click the below button to setup the Facebook plugin with your Facebook app IDs.", MessageType.Warning);
            if (GUILayout.Button("Setup Facebook")) {
                EditorApplication.ExecuteMenuItem("Facebook/Edit Settings");
            }
        }
    }
}
#endif