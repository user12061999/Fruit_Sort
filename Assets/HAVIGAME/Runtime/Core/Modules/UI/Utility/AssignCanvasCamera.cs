using UnityEngine;
using UnityEngine.SceneManagement;

namespace HAVIGAME.UI {
    [AddComponentMenu("HAVIGAME/UI/Assign Canvas Camera")]
    [RequireComponent(typeof(Canvas))]
    public class AssignCanvasCamera : MonoBehaviour {

        private void Start() {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void OnDestroy() {
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1) {
            Assign();
        }

        private void OnEnable() {
            Assign();
        }

        public void Assign() {
            Canvas canvas = GetComponent<Canvas>();

            if (canvas) {
                canvas.worldCamera = Camera.main;
            }
        }
    }
}
