using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public class SpriteProperty : Property<SpriteValue, Sprite> {
        public static SpriteProperty Create() {
            return new SpriteProperty(new DefaultSpriteValue());
        }

        public SpriteProperty(SpriteValue value) : base(value) {

        }
    }

    [System.Serializable]
    public sealed class SpritePropertyReadonly : PropertyReadonly<SpriteValue, Sprite> {
        public static SpritePropertyReadonly Create() {
            return new SpritePropertyReadonly(new DefaultSpriteValue());
        }

        public SpritePropertyReadonly(SpriteValue value) : base(value) {

        }
    }
}