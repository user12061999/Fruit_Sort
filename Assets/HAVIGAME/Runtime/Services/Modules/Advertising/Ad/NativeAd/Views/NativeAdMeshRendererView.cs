using UnityEngine;

namespace HAVIGAME.Services.Advertisings {
    public class NativeAdMeshRendererView : NativeAdTextureElementView {
        [SerializeField] private MeshRenderer meshRenderer;

        public override GameObject RegisterGameObject => meshRenderer.gameObject;

        public override void UpdateElement() {
            if (HasElement && Element.HasData) {
                Texture2D texture = Element.GetData();

                if (texture != null) {
                    meshRenderer.material.mainTexture = texture;
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
