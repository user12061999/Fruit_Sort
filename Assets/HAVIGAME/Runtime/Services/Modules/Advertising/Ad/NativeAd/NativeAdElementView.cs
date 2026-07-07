using HAVIGAME;
using System.Collections.Generic;
using UnityEngine;

namespace HAVIGAME.Services.Advertisings {
    public abstract class NativeAdElementView : MonoBehaviour {
        [SerializeField, ConstantField(typeof(NativeAd.ElementType))] private int elementType;

        public int ElementType => elementType;

        public abstract bool Register(NativeAd nativeAd);

        public abstract void UpdateElement();

        public virtual void Show() {
            gameObject.SetActive(true);

            UpdateElement();
        }

        public virtual void Hide() {
            gameObject.SetActive(false);
        }
    }

    public abstract class NativeAdDoubleElementView : NativeAdElementView {
        private NativeAd.DoubleElement element;

        public bool HasElement => element != null;
        public NativeAd.DoubleElement Element => element;
        public abstract GameObject RegisterGameObject { get; }

        public override bool Register(NativeAd nativeAd) {
            element = nativeAd.GetElement<NativeAd.DoubleElement>(ElementType);

            if (!HasElement) {
                Log.Warning("[NativeAdDoubleElementView] Element {0} no found.", ElementType);
                return false;
            }

            if (Element.Register(RegisterGameObject)) {
                Log.Debug("[NativeAdDoubleElementView] Register element {0} completed.", ElementType);
                return true;
            }
            else {
                Log.Warning("[NativeAdDoubleElementView] Register element {0} failed.", ElementType);
                return false;
            }
        }
    }

    public abstract class NativeAdStringElementView : NativeAdElementView {
        private NativeAd.StringElement element;

        public bool HasElement => element != null;
        public NativeAd.StringElement Element => element;
        public abstract GameObject RegisterGameObject { get; }

        public override bool Register(NativeAd nativeAd) {
            element = nativeAd.GetElement<NativeAd.StringElement>(ElementType);

            if (!HasElement) {
                Log.Warning("[NativeAdStringElementView] Element {0} no found.", ElementType);
                return false;
            }

            if (Element.Register(RegisterGameObject)) {
                Log.Debug("[NativeAdStringElementView] Register element {0} completed.", ElementType);
                return true;
            }
            else {
                Log.Warning("[NativeAdStringElementView] Register element {0} failed.", ElementType);
                return false;
            }
        }
    }

    public abstract class NativeAdTextureElementView : NativeAdElementView {
        private NativeAd.TextureElement element;

        public bool HasElement => element != null;
        public NativeAd.TextureElement Element => element;
        public abstract GameObject RegisterGameObject { get; }

        public override bool Register(NativeAd nativeAd) {
            element = nativeAd.GetElement<NativeAd.TextureElement>(ElementType);

            if (!HasElement) {
                Log.Warning("[NativeAdTextureElementView] Element {0} no found.", ElementType);
                return false;
            }

            if (Element.Register(RegisterGameObject)) {
                Log.Debug("[NativeAdTextureElementView] Register element {0} completed.", ElementType);
                return true;
            }
            else {
                Log.Warning("[NativeAdTextureElementView] Register element {0} failed.", ElementType);
                return false;
            }
        }
    }
}