using HAVIGAME;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "ItemData", menuName = "Game/Data/ItemData")]
public class ItemData : ScriptableObject, IIdentify<int> {
    [SerializeField, ConstantField(typeof(ItemID))] private int id;
    [SerializeField, SpriteField] private Sprite icon;
    [SerializeField] private string displayName;
    [SerializeField, TextArea(3,5)] private string description;
    [SerializeField] private ItemStack price;

    public int Id => id;
    public Sprite Icon => icon;
    public string Name => displayName;
    public string Description => description;
    public ItemStack Price => price;
    public virtual bool Storable => true;

    public virtual void OnUse() {

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ItemData), true)]
    private class ItemEditor : Editor {
        private ItemData data;

        private void OnEnable() {
            data = (ItemData)target;
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height) {
            if (data != null && data.Icon != null) {
                return data.Icon.texture;
            }
            else {
                return base.RenderStaticPreview(assetPath, subAssets, width, height);
            }

        }
    }
#endif
}
