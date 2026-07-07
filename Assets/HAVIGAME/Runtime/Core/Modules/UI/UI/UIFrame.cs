using System;
using UnityEngine;

namespace HAVIGAME.UI {
    [DisallowMultipleComponent]
    [AddComponentMenu("HAVIGAME/UI/UI Frame")]
    public abstract class UIFrame : MonoBehaviour {
        [SerializeField] protected Canvas canvas;
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected UITransition transition;

        protected IFrameManager manager;
        protected bool initialized;
        protected bool showed;
        protected bool paused;
        protected event FrameDelegate onHidden;

        public bool Initialized => initialized;
        public virtual string Name => gameObject.name;
        public bool Showed => showed;
        public bool Paused => paused;
        public bool IsOnTop => manager.Current == this;
        public bool HasTransition => transition != null;
        public bool IsInTransition => transition.IsPlaying;
        public float Alpha {
            get {
                if (canvasGroup) {
                    return canvasGroup.alpha;
                }

                return 1;
            }
            set {
                if (canvasGroup) {
                    canvasGroup.alpha = value;
                }
            }
        }
        public bool Interactable {
            get {
                if (canvasGroup) {
                    return canvasGroup.interactable;
                }

                return true;
            }
            set {
                if (canvasGroup) {
                    canvasGroup.interactable = value;
                }
            }
        }
        public bool BlocksRaycasts {
            get {
                if (canvasGroup) {
                    return canvasGroup.blocksRaycasts;
                }

                return true;
            }
            set {
                if (canvasGroup) {
                    canvasGroup.blocksRaycasts = value;
                }
            }
        }
        public int SortingOrder {
            get {
                if (canvas) {
                    return canvas.sortingOrder;
                }

                return 0;
            }
            set {
                if (canvas) {
                    canvas.sortingOrder = value;
                }
            }
        }

        protected virtual void Reset() {
            canvas = GetComponentInChildren<Canvas>();
            canvasGroup = GetComponentInChildren<CanvasGroup>();
            transition = GetComponentInChildren<UITransition>();
        }

        public virtual void Initialize(IFrameManager manager) {
            if (!Initialized) {
                this.manager = manager;
                this.initialized = true;

                showed = false;
                paused = false;
            }

            if (transition) {
                transition.Initialize();
            }
        }

        public void Show(bool instant = false) {
            if (!Showed) {
                showed = true;
                manager.OnFrameShowed(this);
                OnShow(instant);
            }
        }

        public void Hide(bool instant = false) {
            if (Showed) {
                showed = false;
                manager.OnFrameHidden(this);
                OnHide(instant);
            }
        }

        public void Pause() {
            if (!Paused) {
                paused = true;
                OnPause();
            }
        }

        public void Resume() {
            if (Paused) {
                paused = false;
                OnResume();
            }
        }

        public void Back() {
            OnBack();
        }

        public void OnHiddenCallback(FrameDelegate callback) {
            if (callback != null) this.onHidden += callback;
        }

        public void ClearOnHiddenCallbacks() {
            this.onHidden = null;
        }

        public Delegate[] GetOnHiddenCallbacks() {
            if (this.onHidden != null) {
                return this.onHidden.GetInvocationList();
            }
            return null;
        }

        protected virtual void OnShow(bool instant = false) {
            gameObject.SetActive(true);
            Interactable = false;

            if (!instant && HasTransition) {
                transition.PlayShowAnimation(OnShowCompleted);
            }
            else {
                OnShowCompleted();
            }
        }

        protected virtual void OnHide(bool instant = false) {
            Interactable = false;

            if (!instant && HasTransition) {
                transition.PlayHideAnimation(OnHideCompleted);
            }
            else {
                OnHideCompleted();
            }
        }

        protected virtual void OnShowCompleted() {
            Interactable = true;
        }

        protected virtual void OnHideCompleted() {
            gameObject.SetActive(false);
            InvokeOnHidden();
        }

        protected void InvokeOnHidden() {
            onHidden?.Invoke(this);
            onHidden = null;
        }

        protected virtual void OnPause() { }

        protected virtual void OnResume() { }

        protected virtual void OnBack() {
            Hide();
        }
    }
}
