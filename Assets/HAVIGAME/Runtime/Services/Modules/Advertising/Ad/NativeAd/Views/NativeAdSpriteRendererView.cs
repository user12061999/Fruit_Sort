using UnityEngine;

namespace HAVIGAME.Services.Advertisings {
    public class NativeAdSpriteRendererView : NativeAdTextureElementView {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Vector2 pivot = new Vector2(0.5f, 0.5f);
        [SerializeField] private Vector2Int referenceSize = new Vector2Int(100, 100);

        public override GameObject RegisterGameObject => spriteRenderer.gameObject;

        public override void UpdateElement() {
            if (HasElement && Element.HasData) {
                Texture2D texture = Element.GetData();

                if(texture != null) {
                    int textureWidth = texture.width;
                    int textureHeight = texture.height;

                    float pixelPerUnitsOnWidth = ((float)textureWidth / referenceSize.x) * 100;
                    float pixelPerUnitsOnHeight = ((float)textureHeight / referenceSize.y) * 100;
                    float pixelPerUnits = Mathf.Max(pixelPerUnitsOnWidth, pixelPerUnitsOnHeight);

                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, textureWidth, textureHeight), pivot, pixelPerUnits);
                    spriteRenderer.sprite = sprite;
                    spriteRenderer.color = Color.white;
                    spriteRenderer.drawMode = SpriteDrawMode.Simple;
                    spriteRenderer.transform.localScale = Vector3.one;
                }
                else {
                    Hide();
                }
            }
            else {
                Hide();
            }
        }
    }
}
