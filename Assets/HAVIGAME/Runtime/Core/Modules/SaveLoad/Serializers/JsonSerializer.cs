using System;
using UnityEngine;

namespace HAVIGAME.SaveLoad.Serializers {
    public class JsonSerializer : ISerializer {
        public string Serialize<T>(T obj) {
            string result = default;
            try {
                result = JsonUtility.ToJson(obj);
            }
            catch (Exception ex) {
                Debug.LogException(ex);
            }
            return result;
        }

        public T Deserialize<T>(string contents) {
            T result = default(T);
            try {
                result = JsonUtility.FromJson<T>(contents);
            }
            catch (Exception ex) {
                Debug.LogException(ex);
            }
            return result;
        }
    }
}