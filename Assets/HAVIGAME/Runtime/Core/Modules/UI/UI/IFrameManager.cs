namespace HAVIGAME.UI {
    public interface IFrameManager {
        public UIFrame Current { get; }

        public void OnFrameShowed(UIFrame frame);

        public void OnFrameHidden(UIFrame frame);

    }
}
