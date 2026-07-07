using UnityEngine;

namespace HAVIGAME.Services.RemoteConfig {

    [System.Serializable]
    [CategoryMenu("Remote Config/Remote Bool")]
    public class RemoteConfigBoolValue : BoolValue {
        [SerializeField] private string remoteKey;
        [SerializeField] private bool defaultValue;

        public override bool Get(Args args) {
            return RemoteConfigManager.GetBooleanValue(remoteKey, defaultValue);
        }

        public override bool Set(bool value, Args args) {
            return false;
        }
    }
}
