using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HAVIGAME {
    public interface IIdentify<T> {
        T Id { get; }
    }

    public abstract class Database : ScriptableObject, IDisposable {

        public virtual void Initialize() {

        }

        public static T Load<T>() where T : Database {
            return Resources.Load<T>(typeof(T).Name);
        }

        public static T LoadSettings<T>() where T : Database {
            return Resources.Load<T>(Utility.Text.Format("Settings/{0}", typeof(T).Name));
        }

        public static void Unload<T>(T database) where T : Database {
            database.Dispose();
            Resources.UnloadAsset(database);
        }

        public virtual void Dispose() {

        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Database), true)]
        protected class DatabaseInspector : UnityEditor.Editor {

            public override void OnInspectorGUI() {
                base.OnInspectorGUI();

                Database database = (Database)target;

                if (!Application.isPlaying && database.Installable) {

                    GUILayout.Space(20);

                    if (GUILayout.Button("Install Database")) {
                        if (EditorUtility.DisplayDialog("Install", "Reinstalling data will delete previous data. Do you want to continue?", "Install", "Cancel")) {
                            database.InstallDatabase();
                            EditorUtility.SetDirty(database);
                        }
                    }
                }
            }
        }

        protected virtual bool Installable => false;

        protected virtual void InstallDatabase() {

        }
#endif
    }

    public abstract class Database<T> : Database where T : Database<T> {
        [System.NonSerialized] protected static T instance;

        public static bool Initialized => instance != null;

        public static T Instance {
            get {
                if (!Initialized) {
                    instance = Load<T>();
                    if (instance != null) {
                        Instance.Initialize();
                    } else {
                        Log.Error("[DATABASE] The resource asset {0} no found.", typeof(T).Name);
                    }
                }
                return instance;
            }
            private set { instance = value; }
        }

        public void Create() {

        }

        public override void Dispose() {
            instance = null;
        }
    }

    public abstract class Settings<T> : Database where T : Settings<T> {
        [System.NonSerialized] protected static T instance;

        public static bool Initialized => instance != null;

        public static T Instance {
            get {
                if (!Initialized) {
                    instance = LoadSettings<T>();
                    if (instance != null) {
                        Instance.Initialize();
                    } else {
                        Log.Error("[DATABASE] The resource asset {0} no found.", typeof(T).Name);
                    }
                }
                return instance;
            }
            private set { instance = value; }
        }

        public override void Dispose() {
            instance = null;
        }
    }

    public abstract class Database<T, TData> : Database<T> where T : Database<T, TData> {
        [SerializeField] protected TData[] database;

        public int GetCount() {
            return database.Length;
        }

        public int GetCount<TResult>() where TResult : TData {
            int count = 0;
            foreach (var item in database) {
                if (item is TResult) count++;
            }
            return count;
        }

        public TData GetDataByIndex(int index) {
            if (index < 0 || index >= database.Length) {
                return default;
            } else {
                return database[index];
            }
        }

        public int IndexOf(TData data) {
            return Array.IndexOf(database, data);
        }

        public IEnumerable<TData> GetAll() {
            foreach (var item in database) {
                yield return item;
            }
        }

        public IEnumerable<TResult> GetAll<TResult>() where TResult : TData {
            foreach (var item in database) {
                if (item is TResult result) yield return result;
            }
        }

        public IEnumerable<TData> GetAll(Func<TData, bool> condition) {
            foreach (var item in database) {
                if (condition.Invoke(item)) yield return item;
            }
        }

        public IEnumerable<TData> GetAll<TResult>(Func<TResult, bool> condition) where TResult : TData {
            foreach (var item in database) {
                if (item is TResult result && condition.Invoke(result)) yield return item;
            }
        }

        public TData this[int index] {
            get {
                return database[index];
            }
        }

#if UNITY_EDITOR
        protected override void InstallDatabase() {
            base.InstallDatabase();

            if (typeof(TData).IsSubclassOf(typeof(UnityEngine.Object))) {
                string[] assetGuids = UnityEditor.AssetDatabase.FindAssets(string.Format("t:{0}", typeof(TData)));

                List<TData> assets = new List<TData>();
                foreach (string guid in assetGuids) {
                    UnityEngine.Object assetObject = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));

                    if (assetObject is TData asset) {
                        assets.Add(asset);
                    }
                }

                database = assets.ToArray();
            }
        }
#endif
    }

    public abstract class Database<T, TId, TData> : Database<T, TData> where T : Database<T, TId, TData> where TData : IIdentify<TId> {
        [System.NonSerialized] private Dictionary<TId, TData> databaseDictionary;

        public override void Initialize() {
            base.Initialize();

            databaseDictionary = new Dictionary<TId, TData>(database.Length);

            foreach (TData data in GetAll()) {
                databaseDictionary[data.Id] = data;
            }
        }

        public bool ConstainsId(TId id) {
            return databaseDictionary.ContainsKey(id);
        }

        public TData GetDataById(TId id) {
            return databaseDictionary[id];
        }

        public bool TryGetDataById(TId id, out TData data) {
            return databaseDictionary.TryGetValue(id, out data);
        }
    }

    public abstract class DatabaseCollection<TData> : Database {
        [SerializeField] protected TData[] database;

        public int GetCount() {
            return database.Length;
        }

        public int GetCount<TResult>() where TResult : TData {
            int count = 0;
            foreach (var item in database) {
                if (item is TResult) count++;
            }
            return count;
        }

        public TData GetDataByIndex(int index) {
            if (index < 0 || index >= database.Length) {
                return default;
            } else {
                return database[index];
            }
        }

        public int IndexOf(TData data) {
            return Array.IndexOf(database, data);
        }

        public IEnumerable<TData> GetAll() {
            foreach (var item in database) {
                yield return item;
            }
        }

        public IEnumerable<TResult> GetAll<TResult>() where TResult : TData {
            foreach (var item in database) {
                if (item is TResult result) yield return result;
            }
        }

        public IEnumerable<TData> GetAll(Func<TData, bool> condition) {
            foreach (var item in database) {
                if (condition.Invoke(item)) yield return item;
            }
        }

        public IEnumerable<TData> GetAll<TResult>(Func<TResult, bool> condition) where TResult : TData {
            foreach (var item in database) {
                if (item is TResult result && condition.Invoke(result)) yield return item;
            }
        }

        public TData this[int index] {
            get {
                return database[index];
            }
        }

#if UNITY_EDITOR
        protected override void InstallDatabase() {
            base.InstallDatabase();

            string[] assetGuids = UnityEditor.AssetDatabase.FindAssets(string.Format("t:{0}", typeof(TData)));

            List<TData> assets = new List<TData>();
            foreach (string guid in assetGuids) {
                UnityEngine.Object assetObject = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));

                if (assetObject is TData asset) {
                    assets.Add(asset);
                }
            }

            database = assets.ToArray();

            EditorUtility.SetDirty(this);
        }
#endif
    }

    public abstract class DatabaseCollection<TId, TData> : DatabaseCollection<TData> where TData : IIdentify<TId> {
        [System.NonSerialized] private Dictionary<TId, TData> databaseDictionary;

        public override void Initialize() {
            base.Initialize();

            databaseDictionary = new Dictionary<TId, TData>(database.Length);

            foreach (TData data in GetAll()) {
                databaseDictionary[data.Id] = data;
            }
        }

        public bool ConstainsId(TId id) {
            return databaseDictionary.ContainsKey(id);
        }

        public TData GetDataById(TId id) {
            return databaseDictionary[id];
        }

        public bool TryGetDataById(TId id, out TData data) {
            return databaseDictionary.TryGetValue(id, out data);
        }
    }
}
