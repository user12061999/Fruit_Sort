using UnityEngine;

namespace HAVIGAME {
    public static partial class Extensions {
        public static bool HasLayer(this LayerMask layerMask, LayerMask layer) {
            return layerMask == (layerMask | (1 << layer));
        }

        public static bool IsNothing(this LayerMask layerMask) {
            return layerMask == 0;
        }

        public static bool IsEverything(this LayerMask layerMask) {
            return layerMask == ~0;
        }
    }
}
