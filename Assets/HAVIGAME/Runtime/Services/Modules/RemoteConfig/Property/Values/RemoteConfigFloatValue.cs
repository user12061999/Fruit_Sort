using UnityEngine;

namespace HAVIGAME.Services.RemoteConfig {

    [System.Serializable]
    [CategoryMenu("Remote Config/Remote Float")]
    public class RemoteConfigFloatValue : FloatValue {
        [SerializeField] private string remoteKey;
        [SerializeField] private float defaultValue;

        public override float Get(Args args) {
            return RemoteConfigManager.GetFloatValue(remoteKey, defaultValue);
        }

        public override bool Set(float value, Args args) {
            return false;
        }
    }
}
