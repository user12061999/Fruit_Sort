using System;

namespace HAVIGAME.Services.Advertisings {
    public abstract class AppOpenAd : Ad {

        public override AdFormat Format => AdFormat.AppOpenAd;

        public AppOpenAd(AdId id) : base(id) {
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
