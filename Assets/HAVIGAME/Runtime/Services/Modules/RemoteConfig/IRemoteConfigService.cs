using System.Collections.Generic;

namespace HAVIGAME.Services.RemoteConfig {

    public interface IRemoteConfigService {
        public InitializeEvent InitializeEvent { get; }
        public int ValueCount { get; }
        public IEnumerable<KeyValuePair<string, string>> AllValues { get; }
        public void Initialize();
    }

    [System.Serializable]
    public abstract class RemoteConfigServiceProvider : ServiceProvider<IRemoteConfigService> {
        
    }
}
