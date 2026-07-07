using System;

namespace HAVIGAME.Services.Advertisings {
    public abstract class RewardedAd: Ad {

        public override AdFormat Format => AdFormat.RewardedAd;

        public RewardedAd(AdId id) : base(id) {
        }

        public abstract bool Show(Action onCompleted, Action onFailed, string placement);
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
