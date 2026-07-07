using System.Collections.Generic;
using UnityEngine;

namespace HAVIGAME.Services.Advertisings {
    public abstract class NativeImmersiveAd : Ad {
        protected AdAspectRatio[] aspectRatios;
        protected bool canvasMode;
        protected bool clickable;
        protected bool adBadgeEnable;
        protected List<GameObject> friendlyObjects;

        public override AdFormat Format => AdFormat.NativeImmersiveAd;
        public AdAspectRatio[] AspectRatios => aspectRatios;
        public bool CanvasMode => canvasMode;
        public bool Clickable => clickable;
        public bool AdBadgeEnable => adBadgeEnable;
        public List<GameObject> FriendlyObjects => friendlyObjects;


        public NativeImmersiveAd(AdId id) : base(id) {
        }

        public void SetAspectRatio(AdAspectRatio aspectRatio) {
            this.aspectRatios = new AdAspectRatio[] { aspectRatio };
        }

        public void SetAspectRatios(AdAspectRatio[] aspectRatios) {
            this.aspectRatios = aspectRatios;
        }

        public void SetCanvasMode(bool canvasMode) {
            this.canvasMode = canvasMode;
        }

        public void SetClickable(bool clickable) {
            this.clickable = clickable;
        }

        public void SetAdBadge(bool enable) {
            this.adBadgeEnable = enable;
        }

        public void SetFriendlyObjects(List<GameObject> friendlyObjects) {
            this.friendlyObjects = friendlyObjects;
        }

        public abstract bool Show(string placement);
        public abstract bool Hide();
        protected virtual void Reload() {
            Reload(false);
        }
        protected virtual void Reload(bool reset) {
            IsReloading = false;

            if (reset) {
                AdId.Reset();
            } else {
                AdId.Next();
            }
            Load();
        }
        public abstract void SetPosition(Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale);
        public abstract void SetPosition(Vector2 anchorMin, Vector2 anchorMax, Vector2 anchorPosition, Vector2 pivot);
        public abstract void SetMaterial(Material material);
        public abstract void SetCamera(Camera camera);

        protected virtual void OnNativeAdDisplayed() { }
        protected virtual void OnNativeAdDisplayFailed(string error) { }
        protected virtual void OnNativeAdClosed() {  }
    }
}