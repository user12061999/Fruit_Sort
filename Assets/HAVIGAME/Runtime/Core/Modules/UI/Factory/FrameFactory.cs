using System.Collections.Generic;
using UnityEngine;

namespace HAVIGAME.UI {

    [System.Serializable]
    public abstract class FrameFactory {

        public abstract void Initialize(Transform root, int capacity);

        public abstract bool ContainsReleased(UIFrame frame);

        public abstract bool ContainsCollected(UIFrame frame);

        public abstract IEnumerable<UIFrame> GetAllReleased();

        public abstract IEnumerable<UIFrame> GetAllCollected();

        public abstract F Spawn<F>() where F : UIFrame;

        public abstract UIFrame Spawn(string name);

        public abstract void Recycle(UIFrame frame);

        public static UIFrame GetFrame(IEnumerable<UIFrame> frames, string name) {
            foreach (var item in frames) {
                if (item.Name.Equals(name)) return item;
            }

            return null;
        }

        public static F GetFrame<F>(IEnumerable<UIFrame> frames) where F : UIFrame {
            foreach (var item in frames) {
                if (item is F result) return result;
            }

            return null;
        }
    }
}
