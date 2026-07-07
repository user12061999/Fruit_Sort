#if UNITY_IOS

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;
using System.Text;

public class iOSPlistPostProcessBuild {
    [PostProcessBuild]
    public static void ChangeXcodePlist(BuildTarget buildTarget, string basePath) {

#if FIREBASE && FIREBASE_ANALYTICS
        if (buildTarget == BuildTarget.iOS) {
            iOSPlistDocument plistDocument = new iOSPlistDocument(GetPlistPath(basePath));
            plistDocument.SetAllowAnalyticsStorage(true);
            plistDocument.SetAllowAdStorage(true);
            plistDocument.SetAllowAdUserData(true);
            plistDocument.SetAllowAdPersonalizationSignals(true);

            plistDocument.Save();
        }
#endif

    }

    private static string GetPlistPath(string basePath) {
        StringBuilder pathBuilder = new StringBuilder(basePath);
        pathBuilder.Append(Path.DirectorySeparatorChar).Append("Info.plist");
        return pathBuilder.ToString();
    }

    internal class iOSPlistDocument {
        protected string path;
        protected PlistDocument plistDocument;

        public iOSPlistDocument(string path) {
            this.path = path;

            plistDocument = new PlistDocument();
            plistDocument.ReadFromString(File.ReadAllText(path));
        }

        public string Save() {
            return SaveAs(path);
        }

        public string SaveAs(string path) {
            File.WriteAllText(path, plistDocument.WriteToString());
            return path;
        }

        internal void SetBoolean(string key, bool value) {
            plistDocument.root.SetBoolean(key, value);
        }

        internal void SetInteger(string key, int value) {
            plistDocument.root.SetInteger(key, value);
        }

        internal void SetFloat(string key, float value) {
            plistDocument.root.SetReal(key, value);
        }

        internal void SetString(string key, string value) {
            plistDocument.root.SetString(key, value);
        }
        internal void SetDate(string key, System.DateTime value) {
            plistDocument.root.SetDate(key, value);
        }

        internal void SetAllowAnalyticsStorage(bool allow) {
            SetBoolean("GOOGLE_ANALYTICS_DEFAULT_ALLOW_ANALYTICS_STORAGE", allow);
        }

        internal void SetAllowAdStorage(bool allow) {
            SetBoolean("GOOGLE_ANALYTICS_DEFAULT_ALLOW_AD_STORAGE", allow);
        }

        internal void SetAllowAdUserData(bool allow) {
            SetBoolean("GOOGLE_ANALYTICS_DEFAULT_ALLOW_AD_USER_DATA", allow);
        }
        internal void SetAllowAdPersonalizationSignals(bool allow) {
            SetBoolean("GOOGLE_ANALYTICS_DEFAULT_ALLOW_AD_PERSONALIZATION_SIGNALS", allow);
        }
    }
}

#endif