using UnityEngine;

namespace HAVIGAME.Services.Advertisings {
    public class NativeAdStarView : NativeAdDoubleElementView {
        [SerializeField] private Vector2 nomralSize = new Vector2(0.5f, 0.5f);
        [SerializeField] private SpriteRenderer imgContent;

        public override GameObject RegisterGameObject => imgContent.gameObject;

        public override void UpdateElement() {
            float star = 4f;

            if (HasElement && Element.HasData) {
                star = Mathf.Clamp((float)Element.GetData(), 4f, 5f);
            }

            imgContent.drawMode = SpriteDrawMode.Tiled;
            imgContent.size = new Vector2(nomralSize.x * star, nomralSize.y);
        }
    }
}
