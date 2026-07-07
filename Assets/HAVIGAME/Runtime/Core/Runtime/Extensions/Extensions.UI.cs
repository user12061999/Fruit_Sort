using UnityEngine;
using UnityEngine.UI;

namespace HAVIGAME {
    public static partial class Extensions {
        public static void SetAlpha(this Graphic graphic, float alpha) {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        public static void SetAlpha(this Material material, float alpha) {
            Color color = material.color;
            color.a = alpha;
            material.color = color;
        }
    }
}
