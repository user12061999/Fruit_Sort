using UnityEngine;

namespace HAVIGAME.Services.Advertisings {
    [System.Serializable]
    public class AdAspectRatio {
        [SerializeField] private AdAspectRatioType type;
        [SerializeField, Condition("Type", AdAspectRatioType.Custom)] private int width;
        [SerializeField, Condition("Type", AdAspectRatioType.Custom)] private int height;

        public AdAspectRatioType Type => type;
        public int Width => width;
        public int Height => height;

        public AdAspectRatio(AdAspectRatioType type) {
            this.type = type;
            this.width = 0;
            this.height = 0;
        }

        public AdAspectRatio(int width, int height) {
            this.width = width;
            this.height = height;
            this.type = AdAspectRatioType.Custom;
        }

        public bool Equal(AdAspectRatio other) {
            if (this.type == other.type) {
                if (this.type == AdAspectRatioType.Custom) {
                    return this.width == other.width && this.height == other.height;
                } else {
                    return true;
                }
            } else {
                return false;
            }
        }
    }

    [System.Serializable]
    public enum AdAspectRatioType {
        AR_1x1,
        AR_6x5,
        AR_1x2,
        AR_32x5,
        AR_16x5,
        AR_364x45,
        AR_4x5,
        AR_97x25,
        AR_19x10,
        AR_39x5,
        Custom,
    }
}