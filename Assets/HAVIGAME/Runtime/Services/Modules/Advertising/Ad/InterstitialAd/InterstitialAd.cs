using System;

namespace HAVIGAME.Services.Advertisings {
    public abstract class InterstitialAd : Ad {

        public override AdFormat Format => AdFormat.InterstitialAd;

        public InterstitialAd(AdId id) : base(id) {

        }

        public abstract bool Show(Action onCompleted, string placement);
        protected virtual void Reload() {
            Reload(false);
        }
        protected virtual void Reload(bool reset) {
            IsReloading = false;

            if (reset) {
                AdId.Reset();
            }
            else {
                AdId.Next();
            }
            Load();
        }
    }
}
