namespace HAVIGAME.Services {

    [System.Serializable]
    public abstract class ServiceProvider<T> {
        public abstract T GetService();
    }
}
