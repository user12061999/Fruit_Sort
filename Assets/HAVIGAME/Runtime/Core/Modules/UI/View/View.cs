using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

namespace HAVIGAME.UI {
    public abstract class View<TModel> : MonoBehaviour {

        [System.Serializable]
        public class ViewEvent : UnityEvent<TModel> { }

        [SerializeField] private ViewEvent onModelChanged;

        private TModel model;

        public TModel Model => model;
        public ViewEvent OnModelChanged => onModelChanged;

        public View<TModel> SetModel(TModel model) {
            this.model = model;
            OnModelChange(Model);
            OnModelChanged?.Invoke(Model);
            return this;
        }

        public abstract void Show();

        public virtual void SetActive(bool active) {
            gameObject.SetActive(active);
        }

        protected virtual void OnModelChange(TModel model) { }
    }

    public sealed class CollectionView<TView, TModel> where TView : View<TModel> {
        private Func<TModel, TView> viewCreator;
        private TView prefab;
        private Transform root;
        private List<TView> views;
        private TModel[] models;

        private Action<TView> onViewShowed;

        public CollectionView(TView prefab, Transform root, Func<TModel, TView> viewCreator = null) {
            this.prefab = prefab;
            this.root = root;

            if (viewCreator != null) {
                this.viewCreator = viewCreator;
            }
            else {
                this.viewCreator = DefaultViewCreator;
            }
        }

        public int ModelCount => models != null ? models.Length : 0;

        public int ViewCount => views != null ? views.Count : 0;

        public IEnumerable<TModel> GetModels() {
            return models;
        }

        public IEnumerable<TView> GetViews() {
            for (int i = 0; i < ModelCount; i++) {
                yield return views[i];
            }
        }

        public TModel GetModel(int index) {
            if (models != null && index >= 0 && index < models.Length) {
                return models[index];
            }
            return default;
        }

        public TView GetView(int index) {
            if (views != null && index >= 0 && index < views.Count) {
                return views[index];
            }
            return null;
        }

        public CollectionView<TView, TModel> SetModels(TModel[] models) {
            this.models = models;
            return this;
        }

        public CollectionView<TView, TModel> SetModels(IEnumerable<TModel> models) {
            SetModels(models.ToArray());
            return this;
        }

        public CollectionView<TView, TModel> SetModels(List<TModel> models) {
            SetModels(models.ToArray());
            return this;
        }

        public void Show() {
            if (models == null) {
                Hide();
                return;
            }

            if (views == null) {
                views = new List<TView>(models.Length);
            }

            for (int i = 0; i < models.Length; i++) {
                TModel model = models[i];

                if (i >= views.Count) {
                    views.Add(viewCreator.Invoke(model));
                }

                TView view = views[i];
                view.SetActive(true);
                view.SetModel(model);
                view.Show();
                onViewShowed?.Invoke(view);
            }

            for (int i = views.Count - 1; i >= models.Length; i--) {
                TView view = views[i];
                view.SetActive(false);
            }
        }

        public void Hide() {
            if (views != null) {
                foreach (TView view in views) {
                    view.SetActive(false);
                }
            }
        }

        public void OnViewShow(Action<TView> onViewShowed) {
            this.onViewShowed = onViewShowed;
        }

        private TView DefaultViewCreator(TModel model) {
            return UnityEngine.Object.Instantiate(prefab, root);
        }
    }
}
