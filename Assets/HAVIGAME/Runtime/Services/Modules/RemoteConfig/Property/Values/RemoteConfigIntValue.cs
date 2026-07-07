using UnityEngine;

namespace HAVIGAME.Services.RemoteConfig {

    [System.Serializable]
    [CategoryMenu("Remote Config/Remote Int")]
    public class RemoteConfigIntValue : IntValue {
        [SerializeField] private string remoteKey;
        [SerializeField] private int defaultValue;

        public override int Get(Args args) {
            return RemoteConfigManager.GetIntValue(remoteKey, defaultValue);
        }

        public override bool Set(int value, Args args) {
            return false;
        }
    }
}
