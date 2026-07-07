using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HAVIGAME.UI {
    public class UIStateTransition : UITransition {
        [SerializeReference, Subclass] private UIState[] states;

        public override bool IsPlaying => false;

        public override void Initialize() {
            foreach (var state in states) {
                state.Initialize();
            }
        }

        public override void PlayHideAnimation(Action onCompleted) {
            foreach (var state in states) {
                state.Hide();
            }

            onCompleted?.Invoke();
        }

        public override void PlayShowAnimation(Action onCompleted) {
            foreach (var state in states) {
                state.Show();
            }

            onCompleted?.Invoke();
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(UIStateTransition))]
        protected class UIStateTransitionEditor : Editor {
            private UIStateTransition transition;

            private void OnEnable() {
                transition = (UIStateTransition)target;
            }

            public override void OnInspectorGUI() {
                base.OnInspectorGUI();

                if (GUILayout.Button("Show")) {
                    transition.PlayShowAnimation(null);
                    EditorUtility.SetDirty(transition.gameObject);
                }

                if (GUILayout.Button("Hide")) {
                    transition.PlayHideAnimation(null);
                    EditorUtility.SetDirty(transition.gameObject);
                }
            }
        }

#endif
    }
}