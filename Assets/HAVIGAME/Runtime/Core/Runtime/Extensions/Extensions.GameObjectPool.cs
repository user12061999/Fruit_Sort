using UnityEngine;

namespace HAVIGAME {
    public static partial class Extensions {
        public static T Spawn<T>(this T prefab) where T : Component {
            return GameObjectPool.Instance.Spawn(prefab);
        }

        public static T Spawn<T>(this T prefab, int capacity) where T : Component {
            return GameObjectPool.Instance.Spawn(prefab, true, capacity);
        }

        public static T Spawn<T>(this T prefab, Vector3 position) where T : Component {
            T instance = GameObjectPool.Instance.Spawn(prefab);
            instance.transform.position = position;
            return instance;
        }

        public static T Spawn<T>(this T prefab, Vector3 position, int capacity) where T : Component {
            T instance = GameObjectPool.Instance.Spawn(prefab,true, capacity);
            instance.transform.position = position;
            return instance;
        }

        public static T Spawn<T>(this T prefab, Vector3 position, Quaternion quaternion) where T : Component {
            T instance = GameObjectPool.Instance.Spawn(prefab);
            instance.transform.SetPositionAndRotation(position, quaternion);
            return instance;
        }

        public static T Spawn<T>(this T prefab, Vector3 position, Quaternion quaternion, Transform parent) where T : Component {
            T instance = GameObjectPool.Instance.Spawn(prefab);
            instance.transform.SetPositionAndRotation(position, quaternion);
            instance.transform.SetParent(parent, true);
            return instance;
        }

        public static GameObject Spawn(this GameObject prefab, int capacity) {
            return GameObjectPool.Instance.Spawn(prefab, true, capacity);
        }

        public static GameObject Spawn(this GameObject prefab) {
            return GameObjectPool.Instance.Spawn(prefab);
        }

        public static GameObject Spawn(this GameObject prefab, Vector3 position) {
            GameObject instance = GameObjectPool.Instance.Spawn(prefab);
            instance.transform.position = position;
            return instance;
        }

        public static GameObject Spawn(this GameObject prefab, Vector3 position, Quaternion quaternion) {
            GameObject instance = GameObjectPool.Instance.Spawn(prefab);
            instance.transform.SetPositionAndRotation(position, quaternion);
            return instance;
        }

        public static GameObject Spawn(this GameObject prefab, Vector3 position, Quaternion quaternion, Transform parent) {
            GameObject instance = GameObjectPool.Instance.Spawn(prefab);
            instance.transform.SetPositionAndRotation(position, quaternion);
            instance.transform.SetParent(parent, true);
            return instance;
        }

        public static void Recycle<T>(this T target) where T : Component {
            GameObjectPool.Instance.Recycle(target);
        }

        public static void Recycle(this GameObject target) {
            GameObjectPool.Instance.Recycle(target);
        }
    }
}