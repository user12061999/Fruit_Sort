using System.IO;
using UnityEditor;

using UnityEngine;

namespace HAVIGAME.Editor {
    [UnityEditor.AssetImporters.ScriptedImporter(1, "tsv")]
    public class TSVImporter : UnityEditor.AssetImporters.ScriptedImporter {
        public override void OnImportAsset(UnityEditor.AssetImporters.AssetImportContext importContext) {
            TextAsset textAsset = new TextAsset(File.ReadAllText(importContext.assetPath));
            importContext.AddObjectToAsset(Path.GetFileNameWithoutExtension(importContext.assetPath), textAsset);
            importContext.SetMainObject(textAsset);
            AssetDatabase.SaveAssets();
        }
    }
}