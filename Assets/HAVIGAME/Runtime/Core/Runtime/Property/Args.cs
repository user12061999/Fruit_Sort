using System;
using UnityEngine;

namespace HAVIGAME {

    public class Args: IReferencePoolable {
        public static readonly Args EMPTY = new Args();

        [NonSerialized] private GameObject self;
        [NonSerialized] private GameObject target;

        public GameObject Self => self;
        public GameObject Target => target;

        public Args() {
            this.self = null;
            this.target = null;
        }

        public Args(Component target) : this(target, target) { }

        public Args(GameObject target) : this(target, target) { }

        public Args(Component self, Component target) : this() {
            this.self = self != null ? self.gameObject : null;
            this.target = target != null ? target.gameObject : null;
        }

        public Args(GameObject self, GameObject target) : this() {
            this.self = self;
            this.target = target;
        }

        public void ChangeSelf(GameObject self) {
            if (this.Self == self) return;

            this.self = self;
        }

        public void ChangeSelf<T>(T self) where T : Component {
            this.ChangeSelf(self != null ? self.gameObject : null);
        }

        public void ChangeTarget(GameObject target) {
            if (this.Target == target) return;

            this.target = target;
        }

        public void ChangeTarget<T>(T target) where T : Component {
            this.ChangeTarget(target != null ? target.gameObject : null);
        }

        public virtual void Clear() {
            self = null;
            target = null;
        }
    }
}