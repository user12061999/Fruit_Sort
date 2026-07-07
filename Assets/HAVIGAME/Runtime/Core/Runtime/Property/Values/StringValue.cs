using UnityEngine;

namespace HAVIGAME {

    [System.Serializable]
    public abstract class StringValue : Value<string> { }


    [System.Serializable]
    [CategoryMenu("String", -100)]
    public class DefaultStringValue : StringValue {
        [SerializeField] private string value;

        public override string Get(Args args) {
            return this.value;
        }

        public override bool Set(string value, Args args) {
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
    [CategoryMenu("Text Area", -100)]
    public class TextAreaValue : StringValue {
        [SerializeField, TextArea(1, 3)] private string value;

        public override string Get(Args args) {
            return this.value;
        }

        public override bool Set(string value, Args args) {
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
    [CategoryMenu("Application/Version")]
    public class ApplicationVersionStringValue : StringValue {
        public override string Get(Args args) {
            return Application.version;
        }

        public override bool Set(string value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Application/Unity Version")]
    public class ApplicationUnityVersionStringValue : StringValue {
        public override string Get(Args args) {
            return Application.unityVersion;
        }

        public override bool Set(string value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Application/Identifier")]
    public class ApplicationIdentifierStringValue : StringValue {
        public override string Get(Args args) {
            return Application.identifier;
        }

        public override bool Set(string value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Application/Product Name")]
    public class ApplicationProductNameStringValue : StringValue {
        public override string Get(Args args) {
            return Application.productName;
        }

        public override bool Set(string value, Args args) {
            return false;
        }
    }


    [System.Serializable]
    [CategoryMenu("Path/Data Path")]
    public class ApplicationDataPathStringValue : StringValue {
        public override string Get(Args args) {
            return Application.dataPath;
        }

        public override bool Set(string value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Path/Streaming Assets Path")]
    public class ApplicationStreamingAssetsPathStringValue : StringValue {
        public override string Get(Args args) {
            return Application.streamingAssetsPath;
        }

        public override bool Set(string value, Args args) {
            return false;
        }
    }

    [System.Serializable]
    [CategoryMenu("Path/Persistent Data Path")]
    public class ApplicationPersistentDataPathStringValue : StringValue {
        public override string Get(Args args) {
            return Application.persistentDataPath;
        }

        public override bool Set(string value, Args args) {
            return false;
        }
    }
}