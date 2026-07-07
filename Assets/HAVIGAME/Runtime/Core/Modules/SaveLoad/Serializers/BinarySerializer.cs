using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace HAVIGAME.SaveLoad.Serializers {
    public class BinarySerializer : ISerializer {
        public string Serialize<T>(T obj) {
            string result = default;
            try {
                BinaryFormatter formatter = new BinaryFormatter();

                using (MemoryStream stream = new MemoryStream()) {
                    formatter.Serialize(stream, obj);
                    result = Convert.ToBase64String(stream.ToArray());
                }
            }
            catch (Exception ex) {
                Log.Error(ex.Message);
            }
            return result;
        }

        public T Deserialize<T>(string contents) {
            T result = default(T);
            try {
                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(contents))) {
                    result = (T)formatter.Deserialize(stream);
                }
            }
            catch (Exception ex) {
                Log.Error(ex.Message);
            }
            return result;
        }
    }
}