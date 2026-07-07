using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using DG.Tweening;
using HAVIGAME.Audios;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class GameButton : Button, IPointerDownHandler, IPointerUpHandler {
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private Vector3 scale = new Vector3(1.1f, 1.1f, 1.1f);
    [SerializeField] private Audio pressAudio;

    private Tween tween;

#if UNITY_EDITOR
    protected override void Reset() {
        base.Reset();
        background = GetComponent<Image>();
        icon = GetComponentInChildren<Image>();
        title = GetComponentInChildren<TextMeshProUGUI>();
    }
#endif

    public void SetBackground(Sprite sprite) {
        if (background != null) background.overrideSprite = sprite;
    }

    public void SetIcon(Sprite sprite) {
        if (icon != null) icon.overrideSprite = sprite;
    }

    public void SetTitle(string text) {
        if (title != null) title.text = text;
    }

    public override void OnPointerClick(PointerEventData eventData) {
        base.OnPointerClick(eventData);

        if (!pressAudio.IsEmpty) {
            pressAudio.Play();
        }
        else {
            ConfigDatabase.Instance.DefaultButtonPressAudio.Play();
        }
    }

    public override void OnPointerDown(PointerEventData eventData) {
        base.OnPointerDown(eventData);

        if (!interactable) return;

        DoScale(scale);
    }

    public override void OnPointerUp(PointerEventData eventData) {
        base.OnPointerUp(eventData);

        if (!interactable) return;

        DoScale(Vector3.one);
    }

    private void DoScale(Vector3 scale) {
        tween?.Kill();

        tween = transform.DOScale(scale, 0.1f);
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(GameButton)), CanEditMultipleObjects]
    protected class GameButtonEditor : UnityEditor.UI.ButtonEditor {
        private SerializedProperty backgroundProperty;
        private SerializedProperty iconProperty;
        private SerializedProperty titleProperty;
        private SerializedProperty scaleProperty;
        private SerializedProperty audioProperty;

        protected override void OnEnable() {
            base.OnEnable();

            backgroundProperty = base.serializedObject.FindProperty("background");
            iconProperty = base.serializedObject.FindProperty("icon");
            titleProperty = base.serializedObject.FindProperty("title");
            scaleProperty = base.serializedObject.FindProperty("scale");
            audioProperty = base.serializedObject.FindProperty("pressAudio");
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            base.serializedObject.Update();
            EditorGUILayout.PropertyField(backgroundProperty);
            EditorGUILayout.PropertyField(iconProperty);
            EditorGUILayout.PropertyField(titleProperty);
            EditorGUILayout.PropertyField(scaleProperty);
            EditorGUILayout.PropertyField(audioProperty);

            base.serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
