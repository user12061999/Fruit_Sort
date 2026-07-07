namespace HAVIGAME.SaveLoad {
    public interface ISaveData {
        bool IsChanged { get; }
        void OnCreate();
        void OnDestroy();
        void OnBeforeSave();
        void OnAfterLoad();
    }

    [System.Serializable]
    public class SaveData : ISaveData {
        [System.NonSerialized] private bool isChanged;

        public virtual bool IsChanged => isChanged;

        public SaveData() {
            isChanged = false;
        }

        public virtual void OnCreate() {

        }

        public virtual void OnDestroy() {

        }

        public virtual void OnBeforeSave() {
            isChanged = false;
        }

        public virtual void OnAfterLoad() {

        }

        protected void SetChanged() {
            isChanged = true;
        }
    }
}
