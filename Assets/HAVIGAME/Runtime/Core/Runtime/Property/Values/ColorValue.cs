using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public abstract class ColorValue : Value<Color> { }


    [System.Serializable]
    [CategoryMenu("Color", -100)]
    public class DefaultColorValue : ColorValue {
        [SerializeField] private Color value;

        public override Color Get(Args args) {
            return this.value;
        }

        public override bool Set(Color value, Args args) {
            if (this.value != value) {
                this.value = value;
                return true;
            }
            else {
                return false;
            }
        }
    }


    [System.Serializable]
    [CategoryMenu("Constant/Black")]
    public class BlackColorValue : ColorValue {

        public override Color Get(Args args) {
            return Color.black;
        }

        public override bool Set(Color value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Constant/White")]
    public class WhiteColorValue : ColorValue {

        public override Color Get(Args args) {
            return Color.white;
        }

        public override bool Set(Color value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Constant/Clear")]
    public class ClearColorValue : ColorValue {

        public override Color Get(Args args) {
            return Color.clear;
        }

        public override bool Set(Color value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Constant/Red")]
    public class RedColorValue : ColorValue {

        public override Color Get(Args args) {
            return Color.red;
        }

        public override bool Set(Color value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Constant/Green")]
    public class GreenColorValue : ColorValue {

        public override Color Get(Args args) {
            return Color.green;
        }

        public override bool Set(Color value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Constant/Yellow")]
    public class YellowColorValue : ColorValue {

        public override Color Get(Args args) {
            return Color.yellow;
        }

        public override bool Set(Color value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Constant/Blue")]
    public class BlueColorValue : ColorValue {

        public override Color Get(Args args) {
            return Color.blue;
        }

        public override bool Set(Color value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Constant/Cyan")]
    public class CyanColorValue : ColorValue {

        public override Color Get(Args args) {
            return Color.cyan;
        }

        public override bool Set(Color value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Constant/Gray")]
    public class GrayColorValue : ColorValue {

        public override Color Get(Args args) {
            return Color.gray;
        }

        public override bool Set(Color value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Constant/Magenta")]
    public class MagentaColorValue : ColorValue {

        public override Color Get(Args args) {
            return Color.magenta;
        }

        public override bool Set(Color value, Args args) {
            return false;
        }
    }


    [System.Serializable]
    [CategoryMenu("Rendering/Renderer")]
    public class RendererColorValue : ColorValue {
        [SerializeField] private Renderer value;

        public override Color Get(Args args) {
            return this.value.material.color;
        }

        public override bool Set(Color value, Args args) {
            if (this.value.material.color != value) {
                this.value.material.color = value;
                return true;
            }
            else {
                return false;
            }
        }
    }

    [System.Serializable]
    [CategoryMenu("Rendering/Material")]
    public class MaterialColorValue : ColorValue {
        [SerializeField] private Material value;

        public override Color Get(Args args) {
            return this.value.color;
        }

        public override bool Set(Color value, Args args) {
            if (this.value.color != value) {
                this.value.color = value;
                return true;
            }
            else {
                return false;
            }
        }
    }
}