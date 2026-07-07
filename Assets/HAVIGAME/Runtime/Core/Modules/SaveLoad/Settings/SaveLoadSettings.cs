using UnityEngine;

namespace HAVIGAME.SaveLoad {

    [SettingMenu( typeof(SaveLoadSettings), "Generic/Save & Load", "", null, 2, "Icons/icon_save.psd")]
    [CreateAssetMenu(menuName = "HAVIGAME/Settings/Save Load", fileName = "SaveLoadSettings")]
    public class SaveLoadSettings : Settings<SaveLoadSettings> {

        [Header("[Initialize]")]
        [SerializeField] private int initializeCapacity = 8;

        [Header("[Options]")]
        [SerializeField] private SavePathType path = SavePathType.PersistentDataPath;
        [SerializeField] private SerializerType serializer = SerializerType.Json;
        [SerializeField] private StorageType storage = StorageType.FileSystem;
        [SerializeField] private EncodingType encoding = EncodingType.UTF8;
        [SerializeField] private AutoSaveType autoSave = AutoSaveType.OnApplicationQuit;

        public int InitializeCapacity => initializeCapacity;
        public SavePathType Path => path;
        public SerializerType Serializer => serializer;
        public StorageType Storage => storage;
        public EncodingType Encoding => encoding;
        public AutoSaveType AutoSave => autoSave;

    }
}
