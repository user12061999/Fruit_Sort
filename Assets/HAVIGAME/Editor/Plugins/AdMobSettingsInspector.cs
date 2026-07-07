#if ADMOB

using UnityEngine;
using UnityEditor;

namespace HAVIGAME.Plugins.AdMob.Editor {
    [CustomEditor(typeof(AdMobSettings))]
    public class AdMobSettingsInspector : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            GUILayout.Space(5);

            EditorGUILayout.HelpBox("IMPORTANT: Click the below button to setup the Google Mobile Ads plugin with your AdMob app IDs.", MessageType.Warning);
            if (GUILayout.Button("Setup Google Mobile Ads")) {
                EditorApplication.ExecuteMenuItem("Assets/Google Mobile Ads/Settings...");
            }
        }
    }
}
#endif