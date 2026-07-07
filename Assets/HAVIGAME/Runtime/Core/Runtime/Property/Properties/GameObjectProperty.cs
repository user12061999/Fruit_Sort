using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public class GameObjectProperty : Property<GameObjectValue, GameObject> {
        public static GameObjectProperty Create() {
            return new GameObjectProperty(new DefaultGameObjectValue());
        }

        public GameObjectProperty(GameObjectValue value) : base(value) {

        }
    }

    [System.Serializable]
    public sealed class GameObjectPropertyReadonly : PropertyReadonly<GameObjectValue, GameObject> {
        public static GameObjectPropertyReadonly Create() {
            return new GameObjectPropertyReadonly(new DefaultGameObjectValue());
        }

        public GameObjectPropertyReadonly(GameObjectValue value) : base(value) {

        }
    }
}