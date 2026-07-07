using UnityEngine;

namespace HAVIGAME.Services.Advertisings {
    public abstract class NativeOverlayAd : Ad {
        public override AdFormat Format => AdFormat.NativeOverlayAd;
        public AdPositions Position { get; protected set; }
        public Vector2Int Offset { get; protected set; }
        public NativeOverlayAdStyle Style { get; protected set; }
        public AdSizeOption Size { get; protected set; }
        public abstract float Width { get; }
        public abstract float Height { get; }

        public NativeOverlayAd(AdId id) : base(id) {
            this.Position = AdPositions.Null;
            this.Offset = Vector2Int.zero;
        }

        public abstract bool Show(AdPositions position, Vector2Int offset, NativeOverlayAdStyle style, AdSizeOption size, string placement);
        public abstract void SetPosition(AdPositions position, Vector2Int offset);
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
    }
}
