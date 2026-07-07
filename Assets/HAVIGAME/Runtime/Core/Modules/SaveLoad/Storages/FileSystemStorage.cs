using System.IO;
using System.Text;

namespace HAVIGAME.SaveLoad.Storage {
    public class FileSystemStorage : IStorage {
        public string Read(string path, Encoding encoding) {
            string data = File.ReadAllText(path, encoding);
            return data;
        }

        public void Write(string path, string contents, Encoding encoding) {
            if (!Exits(path)) {
                FileInfo file = new FileInfo(path);
                file.Directory.Create();
            }
            File.WriteAllText(path, contents, encoding);
        }

        public bool Exits(string path) {
            return File.Exists(path);
        }

        public void Delete(string path) {
            File.Delete(path);
        }
    }
}
