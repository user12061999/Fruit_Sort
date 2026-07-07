using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace HAVIGAME.UI {

    [DisallowMultipleComponent]
    [AddComponentMenu("HAVIGAME/UI/UI Manager")]
    public class UIManager : Singleton<UIManager>, IEnumerable<UIFrame>, IFrameManager {

        public static readonly InitializeEvent initializeEvent = new InitializeEvent();

        [System.NonSerialized] private Transform root;
        [System.NonSerialized] private FrameFactory factory;
        [System.NonSerialized] private EventSystem eventSystem;

        public event FrameDelegate onFrameShowed;
        public event FrameDelegate onFrameHidden;

        private Stack<UIFrame> frames;
        private bool autoOrderEnabled;
        private bool physicalBackButtonEnabled;
        private bool clearUIOnSceneLoad;
        private bool raisePauseResumeEvent;

        public override bool IsDontDestroyOnLoad => true;
        public int Count => frames.Count;

        public UIFrame Current => frames.Count > 0 ? frames.Peek() : null;

        public static bool IsInitialized => initializeEvent.IsInitialized;

        protected override void OnAwake() {
            SceneManager.sceneLoaded += OnSceneLoaded;

            GameUISettings settings = GameUISettings.Instance;

            root = new GameObject("Root").transform;
            root.SetParent(transform);

            factory = settings.FrameFactory;
            factory.Initialize(root, settings.InitializeCapacity);

            eventSystem = EventSystem.current;

            if (eventSystem == null) {
                eventSystem = new GameObject("Event System").AddComponent<EventSystem>();
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }

            eventSystem.transform.SetParent(transform);

            frames = new Stack<UIFrame>(settings.InitializeCapacity);
            autoOrderEnabled = settings.OrderUIEnabled;
            physicalBackButtonEnabled = settings.PhysicalBackButton;
            clearUIOnSceneLoad = settings.ClearUIOnSceneLoad;
            raisePauseResumeEvent = true;

            Database.Unload(settings);

            Log.Debug("[UIManager] Initialize completed.");
            initializeEvent.Invoke(true);
        }

#if UNITY_EDITOR || UNITY_ANDROID
        private void Update() {
            if (physicalBackButtonEnabled && Input.GetKeyDown(KeyCode.Escape)) {
                if (Count > 0) {
                    UIFrame frame = Peek();

                    if (frame && frame.Interactable) {
                        frame.Back();
                    }
                }
            }
        }
#endif

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode) {
            if (clearUIOnSceneLoad) {
                Clear(true);
            }
        }

        public void SetInteractable(bool interactable) {
            foreach (UIFrame frame in this) {
                frame.Interactable = interactable;
            }
        }

        public void RaisePauseResumeEvent(bool enable) {
            raisePauseResumeEvent = enable;
        }

        public UIFrame Push(string name, bool instant = false, Action<UIFrame> onBeforeShow = null, FrameDelegate onHideCompleted = null) {
            UIFrame frame = factory.Spawn(name);
            if (frame) {
                Initialize(frame);
                onBeforeShow?.Invoke(frame);
                frame.OnHiddenCallback(onHideCompleted);
                frame.Show(instant);
            } else {
                Log.Error($"[UIManager] UI frame with name {name} no found!");
            }
            return frame;
        }

        public UIFrame PushOverride(string name, bool instant = false, Action<UIFrame> onBeforeShow = null, FrameDelegate onHideCompleted = null) {
            UIFrame top = Peek();

            if (top == null) {
                Log.Error($"[UIManager] Push override failed. Frame stack is empty!");
                return null;
            }

            Delegate[] delegates = top.GetOnHiddenCallbacks();
            top.ClearOnHiddenCallbacks();

            top.Hide(instant);

            UIFrame frame = factory.Spawn(name);
            if (frame) {
                Initialize(frame);
                onBeforeShow?.Invoke(frame);
                frame.OnHiddenCallback(onHideCompleted);
                frame.Show(instant);

                if (delegates != null) {
                    foreach (var item in delegates) {
                        if (item is FrameDelegate frameDelegate) {
                            frame.OnHiddenCallback(frameDelegate);
                        }
                    }
                }
            } else {
                Log.Error($"[UIManager] UI frame with name {name} no found!");
            }
            return frame;
        }

        public F Push<F>(bool instant = false, Action<F> onBeforeShow = null, FrameDelegate onHideCompleted = null) where F : UIFrame {
            F frame = factory.Spawn<F>();
            if (frame) {
                Initialize(frame);
                onBeforeShow?.Invoke(frame);
                frame.OnHiddenCallback(onHideCompleted);
                frame.Show(instant);
            } else {
                Log.Error($"[UIManager] UI frame with type {typeof(F).Name} no found!");
            }
            return frame;
        }

        public F PushOverride<F>(bool instant = false, Action<F> onBeforeShow = null, FrameDelegate onHideCompleted = null) where F : UIFrame {
            UIFrame top = Peek();

            if (top == null) {
                Log.Error($"[UIManager] Push override failed. Frame stack is empty!");
                return null;
            }

            Delegate[] delegates = top.GetOnHiddenCallbacks();
            top.ClearOnHiddenCallbacks();

            top.Hide(instant);

            F frame = factory.Spawn<F>();
            if (frame) {
                Initialize(frame);
                onBeforeShow?.Invoke(frame);
                frame.OnHiddenCallback(onHideCompleted);
                frame.Show(instant);

                if (delegates != null) {
                    foreach (var item in delegates) {
                        if (item is FrameDelegate frameDelegate) {
                            frame.OnHiddenCallback(frameDelegate);
                        }
                    }
                }
            } else {
                Log.Error($"[UIManager] UI frame with type {typeof(F).Name} no found!");
            }
            return frame;
        }

        public UIFrame Peek() {
            if (frames.Count > 0) {
                return frames.Peek();
            }
            return null;
        }

        public UIFrame Pop(bool instant = false, Action<UIFrame> onBeforeHide = null, FrameDelegate onHideCompleted = null) {
            if (frames.Count > 0) {
                UIFrame frame = frames.Peek();
                onBeforeHide?.Invoke(frame);
                frame.OnHiddenCallback(onHideCompleted);
                frame.Hide(instant);
                return frame;
            }
            return null;
        }

        public UIFrame Back() {
            if (frames.Count > 0) {
                UIFrame frame = frames.Peek();
                frame.Back();
                return frame;
            }
            return null;
        }

        public F GetFrameShowed<F>() where F : UIFrame {
            return FrameFactory.GetFrame<F>(factory.GetAllReleased());
        }

        public bool Contains(UIFrame frame) {
            return frames.Contains(frame);
        }

        public void Clear(bool instant = false) {
            while (frames.Count > 0) {
                UIFrame frame = frames.Peek();
                frame.Hide(instant);
            }
        }

        public IEnumerator<UIFrame> GetEnumerator() {
            return frames.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return frames.GetEnumerator();
        }

        private void Initialize(UIFrame frame) {
            if (frame == null)
                return;

            if (!frame.Initialized) {
                frame.Initialize(this);
            }
        }

        public void OnFrameShowed(UIFrame frame) {
            if (frame == null)
                return;

            Log.Info(Utility.Text.Format("[UIManager] {0} showed.", frame.GetType().Name));

            if (raisePauseResumeEvent) {
                UIFrame current = Peek();

                if (current) {
                    current.Pause();
                }
            }

            frames.Push(frame);
            onFrameShowed?.Invoke(frame);

            if (autoOrderEnabled) {
                frame.transform.SetAsLastSibling();

                foreach (UIFrame item in UIManager.Instance) {
                    if (item) {
                        item.SortingOrder = item.transform.GetSiblingIndex();
                    }
                }
            }
        }

        public void OnFrameHidden(UIFrame frame) {
            if (frame == null)
                return;

            Log.Info(Utility.Text.Format("[UIManager] {0} hidden.", frame.GetType().Name));

            frames.Pop();
            factory.Recycle(frame);
            onFrameHidden?.Invoke(frame);

            if (raisePauseResumeEvent) {
                UIFrame current = Peek();

                if (current) {
                    current.Resume();
                }
            }
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {

            public override int Order => EXTEND_MODULE;
            public override InitializeEvent InitializeEvent => UIManager.initializeEvent;

            public override void Initialize() {
                UIManager.Instance.Create();
            }
        }
    }
}
