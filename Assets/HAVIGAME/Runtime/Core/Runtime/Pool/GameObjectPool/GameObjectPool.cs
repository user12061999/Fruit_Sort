using System.Collections.Generic;
using UnityEngine;

namespace HAVIGAME {

    [AddComponentMenu("HAVIGAME/Base/GameObject Pool")]
    public class GameObjectPool : Singleton<GameObjectPool> {

        public class Pool {
            private GameObject prefab;
            private Transform parent;
            private List<GameObject> spawnedGameObjects;
            private Queue<GameObject> recycledGameObjects;

            public GameObject Prefab => prefab;
            public int SpawnedCount => spawnedGameObjects.Count;
            public int RecycledCount => recycledGameObjects.Count;
            public int ToltalCount => SpawnedCount + RecycledCount;

            public Pool(GameObject prefab, int capacity, Transform parent) {
                this.prefab = prefab;
                this.parent = parent;

                recycledGameObjects = new Queue<GameObject>(capacity);
                spawnedGameObjects = new List<GameObject>(capacity);

                for (int i = 0; i < capacity; i++) {
                    GameObject instance = UnityEngine.Object.Instantiate(prefab);
                    instance.gameObject.SetActive(false);
                    instance.transform.SetParent(parent);
                    recycledGameObjects.Enqueue(instance);
                }
            }

            public GameObject Spawn() {
                GameObject instance;
                if (recycledGameObjects.Count > 0) {
                    instance = recycledGameObjects.Dequeue();
                    if (instance == null) {
                        return Spawn();
                    }
                }
                else {
                    instance = UnityEngine.Object.Instantiate(Prefab);
                }

                instance.gameObject.SetActive(true);
                spawnedGameObjects.Add(instance);

                foreach (IGameObjectPoolable poolable in instance.GetComponents<IGameObjectPoolable>()) {
                    poolable.OnSpawned();
                }

                return instance;
            }

            public void Recycle(GameObject target) {
                target.gameObject.SetActive(false);
                target.transform.SetParent(parent);

                if (spawnedGameObjects.Remove(target)) {
                    recycledGameObjects.Enqueue(target);

                    foreach (IGameObjectPoolable poolable in target.GetComponents<IGameObjectPoolable>()) {
                        poolable.OnRecycled();
                    }
                }
            }

            public void DestroyAll() {
                for (int i = 0; i < spawnedGameObjects.Count; i++) {
                    UnityEngine.Object.Destroy(spawnedGameObjects[i].gameObject);
                }
                while (recycledGameObjects.Count > 0) {
                    UnityEngine.Object.Destroy(recycledGameObjects.Dequeue().gameObject);
                }

                spawnedGameObjects.Clear();
                recycledGameObjects.Clear();
            }

            public void DestroyUnusedGameObjects() {
                while (recycledGameObjects.Count > 0) {
                    UnityEngine.Object.Destroy(recycledGameObjects.Dequeue().gameObject);
                }
                recycledGameObjects.Clear();
            }
        }

        public static readonly InitializeEvent initializeEvent = new InitializeEvent();

        private const int initializeCapacity = 64;
        private Dictionary<int, Pool> pools;
        private Dictionary<int, int> prefabs;

        public override bool IsDontDestroyOnLoad => true;
        public static bool IsInitialized => initializeEvent.IsInitialized;


        protected override void OnAwake() {
            base.OnAwake();

            pools = new Dictionary<int, Pool>(initializeCapacity);
            prefabs = new Dictionary<int, int>(initializeCapacity);

            Log.Info("[GameObjectPool] Initialize completed.");
            initializeEvent.Invoke(true);
        }

        public void CreatePool<T>(T target, int capacity) where T : Component {
            CreatePool(target.gameObject, capacity);
        }

        public void CreatePool(GameObject prefab, int capacity) {
            if (HasPool(prefab))
                return;

            pools.Add(prefab.GetInstanceID(), new Pool(prefab, capacity, transform));
        }

        public void DestroyPool<T>(T prefab) where T : Component {
            DestroyPool(prefab.gameObject);
        }

        public void DestroyPool(GameObject prefab) {
            Pool pool;
            if (pools.TryGetValue(prefab.GetInstanceID(), out pool)) {
                pool.DestroyAll();
                pools.Remove(prefab.GetInstanceID());
            }
        }

        public bool HasPool<T>(T prefab) where T : Component {
            return HasPool(prefab.gameObject);
        }

        public bool HasPool(GameObject prefab) {
            return pools.ContainsKey(prefab.GetInstanceID());
        }

        public bool IsPooling<T>(T target) where T : Component {
            return IsPooling(target.gameObject);
        }

        public bool IsPooling(GameObject target) {
            return prefabs.ContainsKey(target.GetInstanceID());
        }

        public GameObject GetPrefab(GameObject target) {
            int prefabId;
            if (prefabs.TryGetValue(target.GetInstanceID(), out prefabId)) {
                Pool pool;
                if (pools.TryGetValue(prefabId, out pool)) {
                    return pool.Prefab;
                }
            }
            return null;
        }

        public T Spawn<T>(T prefab, bool autoPool = true, int capacity = 4) where T : Component {
            return Spawn(prefab.gameObject, autoPool, capacity)?.GetComponent<T>();
        }

        public GameObject Spawn(GameObject prefab, bool autoPool = true, int capacity = 4) {
            if (autoPool && !HasPool(prefab)) {
                CreatePool(prefab, capacity);
            }

            Pool pool;
            if (pools.TryGetValue(prefab.GetInstanceID(), out pool)) {
                GameObject instance = pool.Spawn();
                prefabs[instance.GetInstanceID()] = prefab.GetInstanceID();
                return instance;
            }

            return UnityEngine.Object.Instantiate(prefab);
        }

        public void Recycle<T>(T target) where T : Component {
            Recycle(target.gameObject);
        }

        public void Recycle(GameObject target) {
            if (target == null) {
                return;
            }

            int targetId = target.GetInstanceID();
            int prefabId;
            if (prefabs.TryGetValue(targetId, out prefabId)) {
                Pool pool;
                if (pools.TryGetValue(prefabId, out pool)) {
                    pool.Recycle(target);
                    prefabs.Remove(targetId);
                }
                else {
                    Destroy(target);
                }
            }
            else {
                Destroy(target);
            }
        }

        public void DestroyAll() {
            foreach (var pool in pools) {
                pool.Value.DestroyAll();
            }

            pools.Clear();
        }

        public void DestroyUnusedGameObjects() {
            foreach (var pool in pools) {
                pool.Value.DestroyUnusedGameObjects();
            }
        }



        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }


        public class Initializer : ModuleInitializer {

            public override int Order => CORE_MODULE;
            public override InitializeEvent InitializeEvent => GameObjectPool.initializeEvent;

            public override void Initialize() {
                GameObjectPool.Instance.Create();
            }
        }
    }

}
