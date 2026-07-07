using System;
using System.IO;

namespace HAVIGAME.SaveLoad.Serializers {
    public class XmlSerializer : ISerializer {
        public string Serialize<T>(T obj) {
            string result = default;
            try {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

                using (StringWriter stringWriter = new StringWriter()) {
                    serializer.Serialize(stringWriter, obj);
                    result = stringWriter.ToString();
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
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                using (StringReader stringReader = new StringReader(contents)) {
                    return (T)serializer.Deserialize(stringReader);
                }
            }
            catch (Exception ex) {
                Log.Error(ex.Message);
            }
            return result;
        }
    }
}