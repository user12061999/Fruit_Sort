using System.Text;

namespace HAVIGAME.SaveLoad.Storage {
    public interface IStorage {
        string Read(string path, Encoding encoding);
        void Write(string path, string contents, Encoding encoding);
        bool Exits(string path);
        void Delete(string path);
    }
}
