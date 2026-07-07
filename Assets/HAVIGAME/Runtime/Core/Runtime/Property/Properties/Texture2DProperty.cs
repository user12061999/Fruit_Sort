using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public class Texture2DProperty : Property<Texture2DValue, Texture2D> {
        public static Texture2DProperty Create() {
            return new Texture2DProperty(new DefaultTexture2DValue());
        }

        public Texture2DProperty(Texture2DValue value) : base(value) {

        }
    }

    [System.Serializable]
    public sealed class Texture2DPropertyReadonly : PropertyReadonly<Texture2DValue, Texture2D> {
        public static Texture2DPropertyReadonly Create() {
            return new Texture2DPropertyReadonly(new DefaultTexture2DValue());
        }

        public Texture2DPropertyReadonly(Texture2DValue value) : base(value) {

        }
    }
}