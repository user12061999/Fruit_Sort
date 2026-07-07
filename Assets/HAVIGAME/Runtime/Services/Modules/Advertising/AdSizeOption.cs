using UnityEngine;

namespace HAVIGAME.Services.Advertisings {
    [System.Serializable]
    public class AdSizeOption {
        [SerializeField] private AdSizeType type;
        [SerializeField, Condition("Type", AdSizeType.Custom)] private int width;
        [SerializeField, Condition("Type", AdSizeType.Custom)] private int height;

        public AdSizeType Type => type;
        public int Width => width;
        public int Height => height;

        public AdSizeOption(AdSizeType type) {
            this.type = type;
            this.width = 0;
            this.height = 0;
        }

        public AdSizeOption(int width, int height) {
            this.width = width;
            this.height = height;
            this.type = AdSizeType.Custom;
        }

        public AdSizeOption Scale(float x, float y) {
            float screenDensity = Screen.dpi / 160f;
            float scaledWidth = 0;
            float scaledHeight = 0;
            switch (type) {
                case AdSizeType.Banner:
                    scaledWidth = 320 * screenDensity * x;
                    scaledHeight = 50 * screenDensity * y;
                    break;
                case AdSizeType.MediumRectangle:
                    scaledWidth = 300 * screenDensity * x;
                    scaledHeight = 250 * screenDensity * y;
                    break;
                case AdSizeType.IABBanner:
                    scaledWidth = 468 * screenDensity * x;
                    scaledHeight = 60 * screenDensity * y;
                    break;
                case AdSizeType.Leaderboard:
                    scaledWidth = 728 * screenDensity * x;
                    scaledHeight = 90 * screenDensity * y;
                    break;
                case AdSizeType.AnchoredAdaptive:
                    scaledWidth = Screen.width * x;
                    scaledHeight = Mathf.Min(90 * screenDensity, 0.15f * Screen.height) * y;
                    break;
                default:
                    scaledWidth = this.width * x;
                    scaledHeight = this.height * y;
                    break;
            }
            return new AdSizeOption(Mathf.FloorToInt(scaledWidth), Mathf.FloorToInt(scaledHeight));
        }
    }

    [System.Serializable]
    public enum AdSizeType {
        Banner,
        MediumRectangle,
        IABBanner,
        Leaderboard,
        AnchoredAdaptive,
        Custom,
    }
}
