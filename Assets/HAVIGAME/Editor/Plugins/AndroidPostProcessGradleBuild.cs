#if UNITY_ANDROID

using System.IO;
using System.Text;
using System.Xml;
using UnityEditor.Android;

public class AndroidPostProcessGradleBuild : IPostGenerateGradleAndroidProject {

    public void OnPostGenerateGradleAndroidProject(string basePath) {
#if FIREBASE && FIREBASE_ANALYTICS
        AndroidManifest androidManifest = new AndroidManifest(GetManifestPath(basePath));

        androidManifest.SetAllowAnalyticsStorage(true);
        androidManifest.SetAllowAdStorage(true);
        androidManifest.SetAllowAdUserData(true);
        androidManifest.SetAllowAdPersonalizationSignals(true);

        androidManifest.Save();
#endif
    }

    public int callbackOrder { get { return 1000; } }

    private string GetManifestPath(string basePath) {
        StringBuilder pathBuilder = new StringBuilder(basePath);
        pathBuilder.Append(Path.DirectorySeparatorChar).Append("src");
        pathBuilder.Append(Path.DirectorySeparatorChar).Append("main");
        pathBuilder.Append(Path.DirectorySeparatorChar).Append("AndroidManifest.xml");
        return pathBuilder.ToString();
    }
}

internal class AndroidManifest : XmlDocument {
    public readonly string AndroidXmlNamespace = "http://schemas.android.com/apk/res/android";

    private string path;
    private XmlNamespaceManager namespaceManager;
    private XmlElement ApplicationElement;

    public AndroidManifest(string path) {
        this.path = path;

        using (XmlTextReader reader = new XmlTextReader(path)) {
            reader.Read();
            base.Load(reader);
        }

        namespaceManager = new XmlNamespaceManager(NameTable);
        namespaceManager.AddNamespace("android", AndroidXmlNamespace);
        ApplicationElement = SelectSingleNode("/manifest/application") as XmlElement;
    }

    public string Save() {
        return SaveAs(path);
    }

    public string SaveAs(string path) {
        using (var writer = new XmlTextWriter(path, new UTF8Encoding(false))) {
            writer.Formatting = Formatting.Indented;
            Save(writer);
        }
        return path;
    }

    private XmlAttribute CreateAndroidAttribute(string key, string value) {
        XmlAttribute attribute = CreateAttribute("android", key, AndroidXmlNamespace);
        attribute.Value = value;
        return attribute;
    }

    internal XmlNode GetActivityWithLaunchIntent() {
        return SelectSingleNode("/manifest/application/activity[intent-filter/action/@android:name='android.intent.action.MAIN' and " +
                "intent-filter/category/@android:name='android.intent.category.LAUNCHER']", namespaceManager);
    }

    internal void SetStartingActivityName(string activityName) {
        GetActivityWithLaunchIntent().Attributes.Append(CreateAndroidAttribute("name", activityName));
    }

    internal void SetHardwareAcceleration(bool enable) {
        GetActivityWithLaunchIntent().Attributes.Append(CreateAndroidAttribute("hardwareAccelerated", enable ? "true" : "false"));
    }

    internal void SetMicrophonePermission() {
        var manifest = SelectSingleNode("/manifest");
        XmlElement child = CreateElement("uses-permission");
        manifest.AppendChild(child);
        XmlAttribute newAttribute = CreateAndroidAttribute("name", "android.permission.RECORD_AUDIO");
        child.Attributes.Append(newAttribute);
    }

    internal void SetAllowAnalyticsStorage(bool allow) {
        XmlElement child = CreateElement("meta-data");
        ApplicationElement.AppendChild(child);
        child.Attributes.Append(CreateAndroidAttribute("name", "google_analytics_default_allow_analytics_storage"));
        child.Attributes.Append(CreateAndroidAttribute("value", allow ? "true" : "false"));
    }

    internal void SetAllowAdStorage(bool allow) {
        XmlElement child = CreateElement("meta-data");
        ApplicationElement.AppendChild(child);
        child.Attributes.Append(CreateAndroidAttribute("name", "google_analytics_default_allow_ad_storage"));
        child.Attributes.Append(CreateAndroidAttribute("value", allow ? "true" : "false"));
    }

    internal void SetAllowAdUserData(bool allow) {
        XmlElement child = CreateElement("meta-data");
        ApplicationElement.AppendChild(child);
        child.Attributes.Append(CreateAndroidAttribute("name", "google_analytics_default_allow_ad_user_data"));
        child.Attributes.Append(CreateAndroidAttribute("value", allow ? "true" : "false"));
    }
    internal void SetAllowAdPersonalizationSignals(bool allow) {
        XmlElement child = CreateElement("meta-data");
        ApplicationElement.AppendChild(child);
        child.Attributes.Append(CreateAndroidAttribute("name", "google_analytics_default_allow_ad_personalization_signals"));
        child.Attributes.Append(CreateAndroidAttribute("value", allow ? "true" : "false"));
    }
}

#endif