namespace HAVIGAME.SaveLoad.Serializers {
    public interface ISerializer {
        string Serialize<T>(T obj);
        T Deserialize<T>(string contents);
    }
}