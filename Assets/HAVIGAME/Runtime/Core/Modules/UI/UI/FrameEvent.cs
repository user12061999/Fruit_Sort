using UnityEngine.Events;

namespace HAVIGAME.UI {
    [System.Serializable]
    public class FrameEvent : UnityEvent<UIFrame> { }
    public class TabEvent : UnityEvent<UIFrame, UIFrame> { }

    public delegate void FrameDelegate(UIFrame frame);
    public delegate void TabDelegate(UITab form, UITab to);
}
