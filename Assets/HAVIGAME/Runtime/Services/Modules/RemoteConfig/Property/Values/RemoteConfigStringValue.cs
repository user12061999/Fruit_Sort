using UnityEngine;

namespace HAVIGAME.Services.RemoteConfig {

    [System.Serializable]
    [CategoryMenu("Remote Config/Remote String")]
    public class RemoteConfigStringValue : StringValue {
        [SerializeField] private string remoteKey;
        [SerializeField] private string defaultValue;

        public override string Get(Args args) {
            return RemoteConfigManager.GetStringValue(remoteKey, defaultValue);
        }

        public override bool Set(string value, Args args) {
            return false;
        }
    }
}
