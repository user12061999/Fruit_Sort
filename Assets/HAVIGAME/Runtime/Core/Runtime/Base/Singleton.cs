using UnityEngine;

namespace HAVIGAME {
    public interface ISingletonProvider {
        T CreateInstance<T>() where T : MonoBehaviour;

        public class Resouces : ISingletonProvider {
            public T CreateInstance<T>() where T : MonoBehaviour {
                string type = typeof(T).Name;
                T prefab = Resources.Load<T>(type);

                if (prefab != null) {
                    return UnityEngine.Object.Instantiate(prefab);
                } else {
                    return null;
                }
            }
        }

        public class Lazy : ISingletonProvider {
            public T CreateInstance<T>() where T : MonoBehaviour {
                return new GameObject().AddComponent<T>();
            }
        }
    }

    public abstract class Singleton<T, P> : MonoBehaviour where T : Singleton<T, P> where P : ISingletonProvider, new() {
        private const string singletonNameFormat = "{0} (singleton)";

        private static T instance = null;

        public static T Instance {
            get {
                if (instance == null) {
                    if (!GameManager.IsQuiting) {
                        P provider = new P();
                        provider.CreateInstance<T>();
                    }
                }
                return instance;
            }
        }

        public static bool HasInstance => instance != null;

        public virtual bool IsDontDestroyOnLoad => true;

        public void Create() { }

        protected void Awake() {
            if (instance == null) {
                instance = this as T;
                if (IsDontDestroyOnLoad) DontDestroyOnLoad(gameObject);
                name = Utility.Text.Format(singletonNameFormat, typeof(T).Name);
                OnAwake();
            } else if (instance != this) {
                Log.Warning("[SINGLETON] There is more than one instance of class {0} in the scene.", typeof(T).Name);
                Destroy(this.gameObject);
            }
        }

        protected virtual void OnAwake() {

        }

        protected virtual void OnDestroy() {
            instance = null;
        }
    }

    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T> {
        private const string singletonNameFormat = "{0} (singleton)";

        private static T instance = null;

        public static T Instance {
            get {
                if (instance == null) {
                    if (!GameManager.IsQuiting) {
                        new GameObject().AddComponent<T>();
                    }
                }
                return instance;
            }
        }

        public static bool HasInstance => instance != null;

        public virtual bool IsDontDestroyOnLoad => true;

        public void Create() { }

        protected void Awake() {
            if (instance == null) {
                instance = this as T;
                if (IsDontDestroyOnLoad) DontDestroyOnLoad(gameObject);
                name = Utility.Text.Format(singletonNameFormat, typeof(T).Name);
                OnAwake();
            }
            else if (instance != this) {
                Log.Warning("[SINGLETON] There is more than one instance of class {0} in the scene.", typeof(T).Name);
                Destroy(this.gameObject);
            }
        }

        protected virtual void OnAwake() {

        }

        protected virtual void OnDestroy() {
            instance = null;
        }
    }

}