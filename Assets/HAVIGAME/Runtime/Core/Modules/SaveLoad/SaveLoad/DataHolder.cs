using System.Text;
using HAVIGAME.SaveLoad.Serializers;
using HAVIGAME.SaveLoad.Storage;

namespace HAVIGAME.SaveLoad {


    public abstract class DataHolder {
        public DataHolder() {

        }

        public abstract bool IsNull { get; }
        public abstract bool IsLoaded { get; }

        public abstract void Save(bool force);
        public abstract void Load(bool force);
        public abstract bool Exists();
        public abstract void Delete();
    }

    public sealed class DataHolder<T> : DataHolder where T : ISaveData, new() {
        private string path;
        private ISerializer serializer;
        private IStorage storage;
        private Encoding encoding;

        private T data;
        private int timeLoaded;

        public override bool IsNull => data == null;
        public override bool IsLoaded => timeLoaded > 0;

        public DataHolder(string path, ISerializer serializer, IStorage storage, Encoding encoding) {
            this.path = path;
            this.serializer = serializer;
            this.storage = storage;
            this.encoding = encoding;

            this.data = default;
            this.timeLoaded = 0;
        }

        public T Data {
            get {
                if (IsNull) {
                    Load(false);
                }

                return data;
            }
        }

        public override void Save(bool force) {
            if (!IsLoaded) return;
            if (IsNull) return;
            if (!data.IsChanged && !force) return;

            data.OnBeforeSave();

            string contents = serializer.Serialize<T>(data);
            if (Log.DebugEnabled) Log.Debug("[SaveData] Saving...{0}\n{1}", path, contents);
            storage.Write(path, contents, encoding);
        }

        public override void Load(bool force) {
            if (IsLoaded && !force) return;

            timeLoaded++;

            if (storage.Exits(path)) {
                string contents = storage.Read(path, encoding);
                if (Log.DebugEnabled) Log.Debug("[SaveData] Loading...{0}\n{1}", path, contents);

                if (data != null) {
                    data.OnDestroy();
                }

                data = serializer.Deserialize<T>(contents);
                if (IsNull) {
                    data = new T();
                    data.OnCreate();
                }

                data.OnAfterLoad();
            }
            else {
                data = new T();
                data.OnCreate();
            }
        }

        public override bool Exists() {
            return storage.Exits(path);
        }

        public override void Delete() {
            storage.Delete(path);

            if (data != null) {
                data.OnDestroy();
            }

            data = new T();
            timeLoaded = 1;
        }
    }
}
