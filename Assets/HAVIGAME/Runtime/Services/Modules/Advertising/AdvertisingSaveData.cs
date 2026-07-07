using UnityEngine;
using HAVIGAME.SaveLoad;

namespace HAVIGAME.Services.Advertisings {
    [System.Serializable]
    public class AdvertisingSaveData : SaveData {
        [SerializeField] private bool isRemoveAds;

        public bool IsRemoveAds => isRemoveAds;

        public AdvertisingSaveData() {
            isRemoveAds = false;
        }

        public void SetRemoveAds(bool isRemoveAds) {
            if (this.isRemoveAds != isRemoveAds) {
                this.isRemoveAds = isRemoveAds;
                SetChanged();
            }
        }
    }
}
