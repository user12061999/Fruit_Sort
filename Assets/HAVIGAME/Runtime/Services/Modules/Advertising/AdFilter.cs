using UnityEngine;

namespace HAVIGAME.Services.Advertisings {
    [System.Serializable]
    public struct AdFilter {
        public static readonly AdFilter All = new AdFilter(AdNetwork.All, AdGroup.All);

        [SerializeField] private AdNetwork network;
        [SerializeField] private AdGroup group;

        public AdNetwork Network => network;
        public AdGroup Group => group;

        public AdFilter(AdNetwork network, AdGroup group) {
            this.network = network;
            this.group = group;
        }
    }
}