using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HAVIGAME.SaveLoad.Storage;
using HAVIGAME.SaveLoad.Serializers;
using UnityEngine.SceneManagement;


namespace HAVIGAME.SaveLoad {

    public static class SaveLoadManager {
        public delegate void SaveLoadDelegate();

        public const string rootFolder = "SaveGame";
        public const string defaultFolder = "Default";
        public static readonly InitializeEvent initializeEvent = new InitializeEvent();

        private static string path;
        private static AutoSaveType autoSaveType;
        private static ISerializer serializer;
        private static IStorage storage;
        private static Encoding encoding;
        private static List<DataHolder> saveDatas;

        public static bool IsInitialized => initializeEvent.IsInitialized;

        public static event SaveLoadDelegate onBeforeSave;
        public static event SaveLoadDelegate onAfterSave;

        public static void Initialize() {
            if (initializeEvent.IsRunning) {
                Log.Warning("[SaveLoadManager] SaveLoadManager is running with initialize state {0}.", IsInitialized);
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            GameManager.onApplicationFocus += OnApplicationFocus;
            GameManager.onApplicationPause += OnApplicationPause;
            GameManager.onApplicationQuit += OnApplicationQuit;

            SaveLoadSettings settings = SaveLoadSettings.Instance;

            path = GetPath(settings.Path);
            serializer = GetSerializer(settings.Serializer);
            storage = GetStorage(settings.Storage);
            encoding = GetEncoding(settings.Encoding);
            saveDatas = new List<DataHolder>(settings.InitializeCapacity);
            autoSaveType = settings.AutoSave;

            Database.Unload(settings);

            Log.Debug("[SaveLoadManager] Initialize completed.");
            initializeEvent.Invoke(true);
        }

        private static void OnApplicationFocus(bool focus) {
            if (!focus && autoSaveType.HasFlag(AutoSaveType.OnApplicationFocus)) {
                Save();
            }
        }

        private static void OnApplicationPause(bool pause) {
            if (pause && autoSaveType.HasFlag(AutoSaveType.OnApplicationPause)) {
                Save();
            }
        }

        private static void OnApplicationQuit() {
            if (autoSaveType.HasFlag(AutoSaveType.OnApplicationQuit)) {
                Save();
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode loadMode) {
            if (autoSaveType.HasFlag(AutoSaveType.OnSceneLoad)) {
                Save();
            }
        }

        public static void Save(bool force = false) {
            onBeforeSave?.Invoke();
            foreach (DataHolder saveData in saveDatas) {
                saveData.Save(force);
            }
            onAfterSave?.Invoke();
        }

        public static void Load(bool force = false) {
            foreach (DataHolder saveData in saveDatas) {
                saveData.Load(force);
            }
        }

        public static DataHolder<T> Create<T>(string identifier, string folder = defaultFolder) where T : ISaveData, new() {
            string fullPath = Path.Combine(path, folder, identifier);
            DataHolder<T> saveData = new DataHolder<T>(fullPath, serializer, storage, encoding);
            saveDatas.Add(saveData);
            return saveData;
        }

        public static string[] GetDataFolders() {
            return Directory.GetDirectories(path);
        }

        public static string GetPath(SavePathType type) {
            switch (type) {
                case SavePathType.PersistentDataPath: return Path.Combine(Application.persistentDataPath, rootFolder);
                case SavePathType.DataPath: return Path.Combine(Application.dataPath, rootFolder);
                case SavePathType.StreamingAssetsPath: return Path.Combine(Application.streamingAssetsPath, rootFolder);
                case SavePathType.PlayerPrefs: return rootFolder;
                default: return rootFolder;
            }
        }

        public static Encoding GetEncoding(EncodingType type) {
            switch (type) {
                case EncodingType.UTF8: return Encoding.UTF8;
                case EncodingType.UTF32: return Encoding.UTF32;
                case EncodingType.Unicode: return Encoding.Unicode;
                case EncodingType.ASCII: return Encoding.ASCII;
                default: return Encoding.Default;
            }
        }

        public static ISerializer GetSerializer(SerializerType type) {
            switch (type) {
                case SerializerType.Json: return new JsonSerializer();
                case SerializerType.Xml: return new XmlSerializer();
                case SerializerType.Binary: return new BinarySerializer();
                default: return default;
            }
        }

        public static IStorage GetStorage(StorageType type) {
            switch (type) {
                case StorageType.FileSystem: return new FileSystemStorage();
                case StorageType.PlayerPrefs: return new PlayerPrefsStorage();
                default: return default;
            }
        }



        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {

            public override int Order => EXTEND_MODULE - 1;
            public override InitializeEvent InitializeEvent => SaveLoadManager.initializeEvent;

            public override void Initialize() {
                SaveLoadManager.Initialize();
            }
        }

    }


    [System.Serializable]
    public enum SavePathType {
        PersistentDataPath,
        DataPath,
        StreamingAssetsPath,
        PlayerPrefs,
    }


    [System.Serializable]
    public enum EncodingType {
        UTF8,
        UTF32,
        Unicode,
        ASCII,
    }


    [System.Serializable]
    public enum SerializerType {
        Json,
        Xml,
        Binary,
    }


    [System.Serializable]
    public enum StorageType {
        FileSystem,
        PlayerPrefs,
    }

    [System.Flags]
    [System.Serializable]
    public enum AutoSaveType : byte {
        Never = 0,
        OnApplicationFocus = 1 << 0,
        OnApplicationPause = 1 << 1,
        OnApplicationQuit = 1 << 2,
        OnSceneLoad = 1 << 3,
    }
}