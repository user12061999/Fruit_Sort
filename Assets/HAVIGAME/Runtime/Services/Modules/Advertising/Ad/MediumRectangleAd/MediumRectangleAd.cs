using UnityEngine;

namespace HAVIGAME.Services.Advertisings {
    public abstract class MediumRectangleAd : Ad {
        public override AdFormat Format => AdFormat.MediumRectangleAd;
        public bool Created { get; protected set; }
        public AdPositions Position { get; protected set; }
        public Vector2Int Offset { get; protected set; }
        public abstract float Width { get; }
        public abstract float Height { get; }

        public MediumRectangleAd(AdId id) : base(id) {
            this.Created = false;
            this.Position = AdPositions.Null;
            this.Offset = Vector2Int.zero;
        }

        public abstract bool Show(AdPositions position, Vector2Int offset, string placement);
        public abstract void SetPosition(AdPositions position, Vector2Int offset);
        public abstract bool Hide();
    }
}
