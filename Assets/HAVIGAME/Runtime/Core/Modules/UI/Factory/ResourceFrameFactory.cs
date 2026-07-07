using System.Collections.Generic;
using UnityEngine;

namespace HAVIGAME.UI {

    [System.Serializable]
    [CategoryMenu("Resources")]
    public class ResourceFrameFactory : FrameFactory {

        public const string resourcePathFormat = "{0}/{1}";

        [SerializeField] private string folder = "UI";

        [System.NonSerialized] private Transform root;
        [System.NonSerialized] private List<UIFrame> prefabs;
        [System.NonSerialized] private List<UIFrame> releases;
        [System.NonSerialized] private List<UIFrame> collectes;

        public override void Initialize(Transform root, int capacity) {
            this.root = root;
            prefabs = new List<UIFrame>(capacity);
            releases = new List<UIFrame>(capacity);
            collectes = new List<UIFrame>(capacity);
        }

        public override bool ContainsReleased(UIFrame frame) {
            return releases.Contains(frame);
        }

        public override bool ContainsCollected(UIFrame frame) {
            return collectes.Contains(frame);
        }

        public override IEnumerable<UIFrame> GetAllReleased() {
            foreach (UIFrame frame in releases) {
                yield return frame;
            }
        }

        public override IEnumerable<UIFrame> GetAllCollected() {
            foreach (UIFrame frame in collectes) {
                yield return frame;
            }
        }

        public override UIFrame Spawn(string name) {
            UIFrame instance = GetFrame(collectes, name);
            if (instance) {
                collectes.Remove(instance);
                releases.Add(instance);
                return instance;
            }

            UIFrame prefab = GetFrame(prefabs, name);
            if (prefab) {
                instance = UnityEngine.Object.Instantiate(prefab, root);
                instance.name = prefab.Name;
                releases.Add(instance);
                return instance;
            }

            string path = GetResourcePath(name);
            prefab = Resources.Load<UIFrame>(path);
            if (prefab) {
                prefabs.Add(prefab);
                return Spawn(name);
            }

            return null;
        }

        public override F Spawn<F>() {
            F instance = GetFrame<F>(collectes);
            if (instance) {
                collectes.Remove(instance);
                releases.Add(instance);
                return instance;
            }

            F prefab = GetFrame<F>(prefabs);
            if (prefab) {
                instance = UnityEngine.Object.Instantiate(prefab, root);
                instance.name = prefab.Name;
                releases.Add(instance);
                return instance;
            }

            string path = GetResourcePath<F>();
            prefab = Resources.Load<F>(path);
            if (prefab) {
                prefabs.Add(prefab);
                return Spawn<F>();
            }

            return null;
        }

        public override void Recycle(UIFrame frame) {
            releases.Remove(frame);
            collectes.Add(frame);
        }

        private string GetResourcePath<F>() where F : UIFrame {
            string path = Utility.Text.Format(resourcePathFormat, folder, typeof(F).Name);
            return path;
        }

        private string GetResourcePath(string name) {
            string path = Utility.Text.Format(resourcePathFormat, folder, name);
            return path;
        }
    }
}
