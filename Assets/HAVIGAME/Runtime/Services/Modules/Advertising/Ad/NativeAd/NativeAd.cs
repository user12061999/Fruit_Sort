using System;
using System.Collections.Generic;
using UnityEngine;

namespace HAVIGAME.Services.Advertisings {

    public abstract class NativeAd : Ad {
        private Dictionary<int, Element> elements;

        public override AdFormat Format => AdFormat.NativeAd;

        public NativeAd(AdId id) : base(id) {
            elements = new Dictionary<int, Element>(8);
        }

        public bool HasElement(int elementType) {
            return elements.ContainsKey(elementType);
        }

        public void AddElemnet(Element element) {
            elements[element.ElementType] = element;
        }

        public Element GetElement(int elementType) {
            return elements[elementType];
        }

        public T GetElement<T>(int elementType) where T : Element {
            return elements[elementType] as T;
        }

        public abstract bool Show(NativeAdView view, string placement);

        protected virtual void Reload() {
            Reload(false);
        }
        protected virtual void Reload(bool reset) {
            IsReloading = false;

            if (reset) {
                AdId.Reset();
            }
            else {
                AdId.Next();
            }
            Load();
        }

        public abstract bool Hide();

        public abstract class Element {
            private int elementType;

            public int ElementType => elementType;

            public abstract bool HasData { get; }

            public Element(int elementType) {
                this.elementType = elementType;
            }
        }

        public class Element<T> : Element {
            private Func<T> getter;
            private Func<GameObject, bool> register;

            public override bool HasData => getter != null;

            public Element(int elementType, Func<T> getter, Func<GameObject, bool> register) : base(elementType) {
                this.getter = getter;
                this.register = register;
            }

            public bool Register(GameObject gameObject) {
                try {
                    return register.Invoke(gameObject);
                }
                catch {
                    return false;
                }
            }

            public T GetData() {
                try {
                    return getter.Invoke();
                }
                catch {
                    return default;
                }
            }
        }

        public class StringElement : Element<string> {
            public StringElement(int elementType, Func<string> getter, Func<GameObject, bool> register) : base(elementType, getter, register) { }
        }

        public class DoubleElement : Element<double> {
            public DoubleElement(int elementType, Func<double> getter, Func<GameObject, bool> register) : base(elementType, getter, register) { }
        }

        public class TextureElement : Element<Texture2D> {
            public TextureElement(int elementType, Func<Texture2D> getter, Func<GameObject, bool> register) : base(elementType, getter, register) { }
        }

        public static class ElementType {
            public const int Name = 0;
            public const int Image = 1;
            public const int Description = 2;
            public const int Icon = 3;
            public const int AdChoicesLogo = 4;
            public const int CTA = 5;
            public const int StarRating = 6;
            public const int Store = 7;
            public const int Price = 8;
            public const int Advertiser = 9;
        }
    }

}
