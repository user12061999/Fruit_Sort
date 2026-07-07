using UnityEditor;
using UnityEngine;

namespace HAVIGAME.Editor {
    public static partial class EditorUtility {

        public class HorizontalSplitView {
            private float lastNormalizedPosition;
            private float normalizedPosition;
            private float minNormalizedPosition;
            private float maxNormalizedPosition;
            private Vector2 scrollPosition;
            private bool resize;
            private Rect availableRect;

            public HorizontalSplitView(float normalizedPosition = 0.33f, float minNormalizedPosition = 0f, float maxNormalizedPosition = 1f) {
                this.normalizedPosition = normalizedPosition;
                this.lastNormalizedPosition = normalizedPosition;
                this.minNormalizedPosition = minNormalizedPosition;
                this.maxNormalizedPosition = maxNormalizedPosition;
            }

            public void Begin() {
                Rect tempRect;

                tempRect = EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

                if (tempRect.width > 0.0f) {
                    availableRect = tempRect;
                }
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(availableRect.width * normalizedPosition));
            }

            public void Split() {
                GUILayout.EndScrollView();

                DrawResizeHandle();
            }

            private void DrawResizeHandle() {
                Rect resizeHandleRect;
                Rect expandResizeHandleRect;

                resizeHandleRect = new Rect(availableRect.width * normalizedPosition, availableRect.y, 1, availableRect.height);

                expandResizeHandleRect = new Rect(availableRect.width * normalizedPosition - 8, availableRect.y, 16, availableRect.height);

                GUI.DrawTexture(resizeHandleRect, Texture2D.grayTexture);

                EditorGUIUtility.AddCursorRect(expandResizeHandleRect, MouseCursor.ResizeHorizontal);

                if (Event.current.type == EventType.MouseDrag && expandResizeHandleRect.Contains(Event.current.mousePosition)) {
                    resize = true;
                }

                if (resize) {
                    normalizedPosition = Mathf.Clamp(Event.current.mousePosition.x / availableRect.width, minNormalizedPosition, maxNormalizedPosition);
                }

                if (Event.current.type == EventType.MouseUp) {
                    resize = false;
                }
            }

            public bool End() {
                EditorGUILayout.EndHorizontal();

                if (lastNormalizedPosition != normalizedPosition) {
                    lastNormalizedPosition = normalizedPosition;
                    return true;
                }
                {
                    return false;
                }
            }
        }

        public class VerticalSplitView {
            private Vector2 scrollPosition;
            private float normalizedPosition;
            private bool resize;
            private Rect availableRect;

            public VerticalSplitView(float normalizedPosition = 0.33f) {
                this.normalizedPosition = normalizedPosition;
            }

            public void Begin() {
                Rect tempRect;

                tempRect = EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));

                if (tempRect.width > 0.0f) {
                    availableRect = tempRect;
                }
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(availableRect.height * normalizedPosition));
            }

            public void Split() {
                GUILayout.EndScrollView();

                DrawResizeHandle();
            }

            private void DrawResizeHandle() {
                Rect resizeHandleRect;
                Rect expandResizeHandleRect;

                resizeHandleRect = new Rect(availableRect.x, availableRect.height * normalizedPosition, availableRect.width, 2f);

                expandResizeHandleRect = new Rect(availableRect.width * normalizedPosition - 8, availableRect.y, 16, availableRect.height);

                GUI.DrawTexture(resizeHandleRect, Texture2D.grayTexture);

                EditorGUIUtility.AddCursorRect(expandResizeHandleRect, MouseCursor.ResizeVertical);

                if (Event.current.type == EventType.MouseDrag && expandResizeHandleRect.Contains(Event.current.mousePosition)) {
                    resize = true;
                }

                if (resize) {
                    normalizedPosition = Mathf.Clamp01(Event.current.mousePosition.x / availableRect.width);
                }

                if (Event.current.type == EventType.MouseUp) {
                    resize = false;
                }
            }

            public void End() {
                EditorGUILayout.EndVertical();
            }
        }
    }
}