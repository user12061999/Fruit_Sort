using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public abstract class Texture2DValue : Value<Texture2D> { }


    [System.Serializable]
    [CategoryMenu("Texture2D", -100)]
    public class DefaultTexture2DValue : Texture2DValue {
        [SerializeField] private Texture2D value;

        public override Texture2D Get(Args args) {
            return this.value;
        }

        public override bool Set(Texture2D value, Args args) {
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
    [CategoryMenu("Color", -99)]
    public class ColorTexture2DValue : Texture2DValue {
        [SerializeField] private ColorProperty color;

        [System.NonSerialized] private Texture2D texture2DCache;
        [System.NonSerialized] private Color colorCache;

        public override Texture2D Get(Args args) {
            Color textureColor = color.Get(args);

            if (texture2DCache == null || textureColor != colorCache) {
                colorCache = textureColor;
                texture2DCache = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture2DCache.SetPixel(0, 0, colorCache);
                texture2DCache.Apply();
            }
            return texture2DCache;
        }

        public override bool Set(Texture2D value, Args args) {
            return false;
        }
    }
}