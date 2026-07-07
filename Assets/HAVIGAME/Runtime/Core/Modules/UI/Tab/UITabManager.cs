using UnityEngine;

namespace HAVIGAME.UI {
    public class UITabManager : UIFrame, IFrameManager {
        [Header("[References]")]
        [SerializeField] protected TabConfig[] tabConfigs;

        public event FrameDelegate onFrameShowed;
        public event FrameDelegate onFrameHidden;
        public event TabDelegate onSwitched;

        private UITab current;

        public int Count => tabConfigs.Length;

        public UIFrame Current => current;

        public override void Initialize(IFrameManager manager) {
            base.Initialize(manager);

            foreach (var item in tabConfigs) {
                item.Initialize(this);
            }

            foreach (var item in GetComponents<UITabExtension>()) {
                item.Initialize(this);
            }
        }

        protected override void OnShow(bool instant = false) {
            base.OnShow(instant);

            ShowDefaultTab(instant);
        }

        protected virtual void ShowDefaultTab(bool instant = false) {
            if (current == null) {
                foreach (var item in tabConfigs) {
                    if (item.ShowOnStart) {
                        Switch(item.Button);
                        break;
                    }
                }
            } else {
                current.Show(instant);
            }
        }

        protected override void OnHide(bool instant = false) {
            base.OnHide(instant);

            if (current != null) current.Hide(instant);

            current = null;
        }

        protected override void OnPause() {
            base.OnPause();

            if (current != null) current.Pause();
        }

        protected override void OnResume() {
            base.OnResume();

            if (current != null) current.Resume();
        }

        protected override void OnBack() {
            if (current != null) current.Back();
        }

        public void Switch<F>(bool instant = false) where F : UITab {
            TabConfig config = GetConfigData<F>();

            if (config.Tab == Current) return;

            foreach (var item in tabConfigs) {
                if (item != config) {
                    item.Button.SetIsOnWithoutNotify(false);
                } else {
                    item.Button.SetIsOnWithoutNotify(true);
                }
            }

            UITab from = current;

            if (current != null) {
                current.Hide();
            }

            current = config.Tab;
            current.Show(instant);

            onSwitched?.Invoke(from, current);
        }

        public void Switch(UITab tab, bool instant = false) {
            if (tab == Current) return;

            TabConfig config = GetConfigData(tab);

            foreach (var item in tabConfigs) {
                if (item != config) {
                    item.Button.SetIsOnWithoutNotify(false);
                } else {
                    item.Button.SetIsOnWithoutNotify(true);
                }
            }

            UITab from = current;

            if (current != null) {
                current.Hide(instant);
            }

            current = config.Tab;
            current.Show(instant);

            onSwitched?.Invoke(from, current);
        }

        public void Switch(UITabButton button, bool instant = false) {
            TabConfig config = GetConfigData(button);

            if (config.Tab == Current) return;

            foreach (var item in tabConfigs) {
                if (item != config) {
                    item.Button.SetIsOnWithoutNotify(false);
                } else {
                    item.Button.SetIsOnWithoutNotify(true);
                }
            }

            UITab from = current;

            if (current != null) {
                current.Hide(instant);
            }

            current = config.Tab;
            current.Show(instant);

            onSwitched?.Invoke(from, current);
        }

        public F GetFrame<F>() where F : UIFrame {
            foreach (var item in tabConfigs) {
                if (item.Tab is F result) return result;
            }
            return null;
        }

        public UITabButton GetButtonOf(UITab tab) {
            TabConfig config = GetConfigData(tab);

            if (config != null) return config.Button;
            return null;
        }

        public UIFrame GetTabOf(UITabButton button) {
            TabConfig config = GetConfigData(button);

            if (config != null) return config.Tab;
            return null;
        }

        public void OnFrameShowed(UIFrame frame) {
            if (frame == null)
                return;

            Log.Info(Utility.Text.Format("[UITabManager] {0} showed.", frame.GetType().Name));

            onFrameShowed?.Invoke(frame);
        }

        public void OnFrameHidden(UIFrame frame) {
            if (frame == null)
                return;

            Log.Info(Utility.Text.Format("[UITabManager] {0} hidden.", frame.GetType().Name));

            onFrameHidden?.Invoke(frame);
        }

        internal void OnTabSelect(UITabButton tab) {

            TabConfig config = GetConfigData(current);

            if (config == null || config.Button != tab) {
                Switch(tab);
            }
        }

        private TabConfig GetConfigData(UITabButton button) {
            foreach (var item in tabConfigs) {
                if (item.Button == button) return item;
            }
            return null;
        }

        private TabConfig GetConfigData(UITab frame) {
            foreach (var item in tabConfigs) {
                if (item.Tab == frame) return item;
            }
            return null;
        }

        private TabConfig GetConfigData<F>() where F : UITab {
            foreach (var item in tabConfigs) {
                if (item.Tab is F) return item;
            }
            return null;
        }

        [System.Serializable]
        protected class TabConfig {
            [SerializeField] private UITabButton button;
            [SerializeField] private UITab tab;
            [SerializeField] private bool showOnStart;

            public UITabButton Button => button;
            public UITab Tab => tab;
            public bool ShowOnStart => showOnStart;

            public void Initialize(UITabManager controller) {
                tab.Initialize(controller);
                button.Initialize(controller, showOnStart);
            }
        }
    }
}