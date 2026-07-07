using UnityEngine;

namespace HAVIGAME.Services.Advertisings {
    public abstract class BannerAd : Ad {
        public override AdFormat Format => AdFormat.BannerAd;
        public bool Created { get; protected set; }
        public AdPositions Position { get; protected set; }
        public Vector2Int Offset { get; protected set; }
        public bool Collapsible { get; protected set; }
        public AdSizeOption Size { get; protected set; }
        public abstract float Width { get; }
        public abstract float Height { get; }

        public BannerAd(AdId id, bool collapsible, AdSizeOption size) : base(id) {
            this.Created = false;
            this.Position = AdPositions.BottomCenter;
            this.Offset = Vector2Int.zero;
            this.Collapsible = collapsible;
            this.Size = size;
        }
        public void SetCollapsible(bool collapsible) {
            Collapsible = collapsible;
        }
        public void SetSize(AdSizeOption size) {
            Size = size;
        }
        public abstract bool Show(AdPositions bannerAdPosition, Vector2Int offset, string placement);
        public abstract void SetPosition(AdPositions bannerAdPosition, Vector2Int offset);
        public abstract bool Hide();
    }

    public enum AdPositions {
        TopCenter,
        TopLeft,
        TopRight,
        Centered,
        CenterLeft,
        CenterRight,
        BottomCenter,
        BottomLeft,
        BottomRight,
        Null,
    }
}
