using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HAVIGAME.Scenes {
    public class ScenesManager : Singleton<ScenesManager> {
        public const float maxProgress = 0.9f;

        public static readonly InitializeEvent initializeEvent = new InitializeEvent();

        private Progressing currentProgress;
        private LoadingView[] loadingViews;
        public float minLoadingDuration;

        public bool IsLoading => CurrentProgress.IsLoading;
        public Progressing CurrentProgress => currentProgress;

        protected override void OnAwake() {
            base.OnAwake();

            currentProgress = new Progressing();

            SceneSettings settings = SceneSettings.Instance;

            minLoadingDuration = settings.MinLoadingDuration;

            loadingViews = new LoadingView[settings.LoadingViews.Length];

            for (int i = 0; i < settings.LoadingViews.Length; i++) {
                loadingViews[i] = Instantiate(settings.LoadingViews[i]);
                loadingViews[i].transform.SetParent(transform);
                loadingViews[i].Initialize();
            }

            Database.Unload(settings);

            Log.Debug("[ScenesManager] Initialize completed.");
            initializeEvent.Invoke(true);
        }

        public LoadingView GetLoadingView(int id) {
            foreach (var item in loadingViews) {
                if (item.Id == id) return item;
            }
            return null;
        }


        public void LoadSceneAsyn(int sceneIndex, Action onStarted = null, Action onFinished = null, Func<bool> continueCondition = null) {
            if (IsLoading) {
                Log.Warning("[ScenesManager] Scene is loading.");
                return;
            }

            StartCoroutine(IELoadSceneAsyn(
                () => SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Single)
                , loadingViews[0]
                , onStarted
                , onFinished
                , continueCondition));
        }

        public void LoadSceneAsyn(int sceneIndex, LoadingView loadingView, Action onStarted = null, Action onFinished = null, Func<bool> continueCondition = null) {
            if (IsLoading) {
                Log.Warning("[ScenesManager] Scene is loading.");
                return;
            }

            StartCoroutine(IELoadSceneAsyn(
                () => SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Single)
                , loadingView
                , onStarted
                , onFinished
                , continueCondition));
        }

        public void LoadSceneAsyn(int sceneIndex, int loadingViewId, Action onStarted = null, Action onFinished = null, Func<bool> continueCondition = null) {
            if (IsLoading) {
                Log.Warning("[ScenesManager] Scene is loading.");
                return;
            }

            StartCoroutine(IELoadSceneAsyn(
                () => SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Single)
                , GetLoadingView(loadingViewId)
                , onStarted
                , onFinished
                , continueCondition));
        }


        public void LoadSceneAsyn(string sceneName, Action onStarted = null, Action onFinished = null, Func<bool> continueCondition = null) {
            if (IsLoading) {
                Log.Warning("[ScenesManager] Scene is loading.");
                return;
            }

            StartCoroutine(IELoadSceneAsyn(
               () => SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single)
               , loadingViews[0]
               , onStarted
               , onFinished
               , continueCondition));
        }

        public void LoadSceneAsyn(string sceneName, LoadingView loadingView, Action onStarted = null, Action onFinished = null, Func<bool> continueCondition = null) {
            if (IsLoading) {
                Log.Warning("[ScenesManager] Scene is loading.");
                return;
            }

            StartCoroutine(IELoadSceneAsyn(
                () => SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single)
                , loadingView
                , onStarted
                , onFinished
                , continueCondition));
        }

        public void LoadSceneAsyn(string sceneName, int loadingViewId, Action onStarted = null, Action onFinished = null, Func<bool> continueCondition = null) {
            if (IsLoading) {
                Log.Warning("[ScenesManager] Scene is loading.");
                return;
            }

            StartCoroutine(IELoadSceneAsyn(
                () => SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single)
                , GetLoadingView(loadingViewId)
                , onStarted
                , onFinished
                , continueCondition));
        }


        private IEnumerator IELoadSceneAsyn(Func<AsyncOperation> loader, LoadingView viewer, Action onStarted, Action onFinished, Func<bool> continueCondition = null) {
            currentProgress.OnStart();
            viewer.OnStart();

            yield return StartCoroutine(viewer.FadeOut());
            onStarted?.Invoke();

            yield return StartCoroutine(viewer.OnStartLoading());

            yield return StartCoroutine(viewer.FadeIn());

            AsyncOperation operation = loader?.Invoke();

            operation.allowSceneActivation = false;

            float elapsed = 0f;

            while (!operation.isDone) {

                elapsed += Time.deltaTime;

                float progress = Mathf.Clamp01((operation.progress + elapsed) / (maxProgress + minLoadingDuration));
                currentProgress.OnUpdate(progress);
                viewer.OnLoading(progress);
                
                if (operation.progress >= maxProgress) {
                    break;
                }
                else {
                    yield return null;
                }
            }

            if (continueCondition != null) {
                while (!continueCondition.Invoke()) {
                    elapsed += Time.deltaTime;

                    float progress = Mathf.Clamp01((operation.progress + elapsed) / (maxProgress + minLoadingDuration));
                    currentProgress.OnUpdate(progress);
                    viewer.OnLoading(progress);
                    yield return null;
                }
            }

            while (elapsed < minLoadingDuration) {
                elapsed += Time.deltaTime;

                float progress = Mathf.Clamp01((operation.progress + elapsed) / (maxProgress + minLoadingDuration));
                currentProgress.OnUpdate(progress);
                viewer.OnLoading(progress);
                yield return null;
            }

            yield return StartCoroutine(viewer.FadeOut());

            operation.allowSceneActivation = true;
            yield return new WaitUntil(() => operation.isDone);
            onFinished?.Invoke();

            yield return StartCoroutine(viewer.OnFinishLoading());

            yield return StartCoroutine(viewer.FadeIn());

            viewer.OnFinish();
            currentProgress.OnFinish();
        }

        public class Progressing {
            private bool isLoading;
            private float startedTime;
            private float finishedTime;
            private float progress;

            public bool IsLoading => isLoading;
            public float StartedTime => startedTime;
            public float FinishedTime => finishedTime;
            public float ElapsedTime => Time.time - startedTime;
            public float Progress => progress;

            internal Progressing() {
                this.isLoading = false;
                this.startedTime = 0;
                this.finishedTime = 0;
                this.progress = 0;
            }

            internal void OnStart() {
                this.startedTime = Time.time;
                this.progress = 0;
                this.isLoading = true;
            }

            internal void OnUpdate(float progress) {
                this.progress = progress;
            }

            internal void OnFinish() {
                this.finishedTime = Time.time;
                this.isLoading = false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {

            public override int Order => EXTEND_MODULE;
            public override InitializeEvent InitializeEvent => ScenesManager.initializeEvent;

            public override void Initialize() {
                ScenesManager.Instance.Create();
            }
        }
    }
}
