using UnityEngine;
using System;

namespace HAVIGAME.UI {
    public abstract class UITransition : MonoBehaviour {
        public abstract void Initialize();
        public abstract bool IsPlaying { get; }
        public abstract void PlayShowAnimation(Action onCompleted);
        public abstract void PlayHideAnimation(Action onCompleted);
    }
}