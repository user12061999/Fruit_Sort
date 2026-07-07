using System.Text;
using UnityEngine;

namespace HAVIGAME.SaveLoad.Storage {
    public class PlayerPrefsStorage : IStorage {
        public string Read(string path, Encoding encoding) {
            string data = PlayerPrefs.GetString(path);
            return data;
        }

        public void Write(string path, string contents, Encoding encoding) {
            PlayerPrefs.SetString(path, contents);
        }

        public bool Exits(string path) {
            return PlayerPrefs.HasKey(path);
        }

        public void Delete(string path) {
            PlayerPrefs.DeleteKey(path);
        }
    }
}
