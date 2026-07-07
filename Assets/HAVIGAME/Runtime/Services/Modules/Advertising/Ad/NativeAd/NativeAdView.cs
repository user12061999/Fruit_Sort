using System;

namespace HAVIGAME.Services.Advertisings {
    public class NativeAdView {
        private NativeAdElementView[] elementViews;
        private NativeAd nativeAd;
        public Action onNativeAdDisplayed;
        public Action<string> onNativeAdDisplayFailed;

        public bool IsShowing => nativeAd != null;

        public NativeAdView(NativeAdElementView[] elementViews) {
            this.elementViews = elementViews;
            this.nativeAd = null;
            this.onNativeAdDisplayed = null;
            this.onNativeAdDisplayFailed = null;
        }

        public bool Show(NativeAd nativeAd) {
            if (IsShowing) {
                return false;
            }
            this.nativeAd = nativeAd;

            if (Register()) {

                Update();

                onNativeAdDisplayed?.Invoke();
                return true;
            }
            else {
                onNativeAdDisplayFailed?.Invoke("Cannot register with any GameObject.");

                this.nativeAd = null;
                this.onNativeAdDisplayed = null;
                this.onNativeAdDisplayFailed = null;

                return false;
            }
        }

        public bool Hide() {
            if (!IsShowing) {
                return false;
            }

            foreach (NativeAdElementView elementView in elementViews) {
                if (elementView) elementView.Hide();
            }

            this.nativeAd = null;
            this.onNativeAdDisplayed = null;
            this.onNativeAdDisplayFailed = null;

            return true;
        }

        public void Destroy() {
            foreach (NativeAdElementView elementView in elementViews) {
                if (elementView) elementView.Hide();
            }

            this.nativeAd = null;
            this.onNativeAdDisplayed = null;
            this.onNativeAdDisplayFailed = null;
        }

        public bool Register() {
            bool registed = false;

            foreach (NativeAdElementView elementView in elementViews) {
                registed |= elementView.Register(nativeAd);
            }

            return registed;
        }

        public void Update() {
            foreach (NativeAdElementView elementView in elementViews) {
                if (elementView) {
                    elementView.Show();
                }
            }
        }
    }
}