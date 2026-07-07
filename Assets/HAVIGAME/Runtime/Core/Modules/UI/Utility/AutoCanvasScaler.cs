using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HAVIGAME.UI {
    [RequireComponent(typeof(CanvasScaler))]
    [AddComponentMenu("HAVIGAME/UI/Auto Canvas Scaler")]
    public class AutoCanvasScaler : MonoBehaviour {
        [SerializeField] private CanvasScaler canvasScaler;
        [SerializeField] private bool autoScale;
        [SerializeField, Range(0f, 10f)] private float updateRate = 1f;
        [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(1.33f, 0f, 2f, 1f);

        private WaitForSeconds waitUpdate;
        private float currentAspect;

        private void Reset() {
            OnValidate();
        }

        private void OnValidate() {
            canvasScaler = GetComponent<CanvasScaler>();
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        }

        private void Awake() {
            waitUpdate = new WaitForSeconds(updateRate);
        }

        private void OnEnable() {
            if (autoScale) {
                StartCoroutine(AutoUpdate());
            } else {
                CalculatorScale();
            }
        }

        private void OnDisable() {
            StopAllCoroutines();
        }

        public void CalculatorScale() {
            Camera camera = Camera.main;

            if (camera == null) return;
            if (canvasScaler == null) return;
            if (camera.aspect == currentAspect) return;

            currentAspect = camera.aspect;
            canvasScaler.matchWidthOrHeight = curve.Evaluate(currentAspect);
        }

        private IEnumerator AutoUpdate() {
            while (true) {
                CalculatorScale();
                yield return waitUpdate;
            }
        }
    }
}