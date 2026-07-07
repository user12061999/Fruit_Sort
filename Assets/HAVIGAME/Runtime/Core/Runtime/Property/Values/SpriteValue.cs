using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public abstract class SpriteValue : Value<Sprite> { }


    [System.Serializable]
    [CategoryMenu("Sprite", -100)]
    public class DefaultSpriteValue : SpriteValue {
        [SerializeField] private Sprite value;

        public override Sprite Get(Args args) {
            return this.value;
        }

        public override bool Set(Sprite value, Args args) {
            if (this.value != value) {
                this.value = value;
                return true;
            }
            else {
                return false;
            }
        }
    }

    [System.Serializable]
    [CategoryMenu("Texture2D", -99)]
    public class Texture2DSpriteValue : SpriteValue {
        [SerializeField] private Texture2DProperty texture;

        [System.NonSerialized] private Sprite spriteCache;
        [System.NonSerialized] private Texture2D texture2DCache;

        public override Sprite Get(Args args) {
            Texture2D texture2D = texture.Get(args);

            if (spriteCache == null || texture2D != texture2DCache) {
                texture2DCache = texture2D;
                spriteCache = Sprite.Create(texture2DCache, new Rect(0, 0, texture2DCache.width, texture2DCache.height), new Vector2(0.5f, 0.5f), 100);
            }
            return spriteCache;
        }

        public override bool Set(Sprite value, Args args) {
            return false;
        }
    }
}