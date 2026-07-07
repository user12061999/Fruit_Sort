namespace HAVIGAME {
    public delegate void InitializeDelegate(bool isInitialized);

    public sealed class InitializeEvent {
        private bool isRunning;
        private bool isInitialized;
        private event InitializeDelegate onInitialized;

        public bool IsRunning => isRunning;
        public bool IsInitialized => isInitialized;

        public InitializeEvent() {
            isRunning = false;
            isInitialized = false;
            onInitialized = null;
        }

        public void AddListener(InitializeDelegate callback) {
            if (IsRunning) {
                callback?.Invoke(isInitialized);
            }
            else {
                onInitialized += callback;
            }
        }

        public void Invoke(bool isInitialized) {
            if (this.isRunning) return;

            this.isRunning = true;
            this.isInitialized = isInitialized;

            onInitialized?.Invoke(isInitialized);
            onInitialized = null;
        }
    }
}
