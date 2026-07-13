#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FruitSort.EditorTools
{
    [InitializeOnLoad]
    internal static class ConveyorConnectionFixInstaller
    {
        const string Version = "1.0.0";
        const string Root = "Assets/_Game/Editor/ConveyorConnectionFix";
        const string RendererTemplate = Root + "/Templates/ConveyorBeltRenderer.cs.txt";
        const string SwitchTemplate = Root + "/Templates/ConveyorSwitch.cs.txt";
        const string ReadmePath = Root + "/README.txt";
        const string BackupsRoot = Root + "/Backups";
        const string PromptKey = "FruitSort.ConveyorConnectionFix.Prompted." + Version;

        static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        static ConveyorConnectionFixInstaller()
        {
            EditorApplication.delayCall += ShowImportPromptOnce;
        }

        static void ShowImportPromptOnce()
        {
            if (EditorPrefs.GetBool(PromptKey, false)) return;
            if (!AssetExists(RendererTemplate)) return;

            EditorPrefs.SetBool(PromptKey, true);
            int result = EditorUtility.DisplayDialogComplex(
                "FruitSort Conveyor Connection Fix",
                "Package đã được import. Công cụ sẽ sao lưu rồi thay nội dung của " +
                "ConveyorBeltRenderer.cs và ConveyorSwitch.cs hiện có, đồng thời giữ nguyên file .meta/GUID.",
                "Cài đặt ngay",
                "Để sau",
                "Mở hướng dẫn");

            if (result == 0) InstallOrUpdate();
            else if (result == 2) OpenReadme();
        }

        [MenuItem("Tools/FruitSort/Conveyor Connection Fix/Install or Update", priority = 1200)]
        public static void InstallOrUpdate()
        {
            try
            {
                string rendererTarget = FindSingleTarget("ConveyorBeltRenderer.cs", required: true);
                string switchTarget = FindSingleTarget("ConveyorSwitch.cs", required: false);
                if (string.IsNullOrEmpty(rendererTarget)) return;

                string backupDirectory = CreateBackupDirectory();
                var manifest = new List<string>();

                AssetDatabase.StartAssetEditing();
                try
                {
                    ReplaceFromTemplate(rendererTarget, RendererTemplate, backupDirectory,
                        "ConveyorBeltRenderer.cs.txt", manifest);

                    if (!string.IsNullOrEmpty(switchTarget))
                    {
                        ReplaceFromTemplate(switchTarget, SwitchTemplate, backupDirectory,
                            "ConveyorSwitch.cs.txt", manifest);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                File.WriteAllLines(Path.Combine(backupDirectory, "manifest.txt"), manifest, Utf8NoBom);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                string patched = string.IsNullOrEmpty(switchTarget)
                    ? "ConveyorBeltRenderer.cs"
                    : "ConveyorBeltRenderer.cs và ConveyorSwitch.cs";

                EditorUtility.DisplayDialog(
                    "Cài đặt hoàn tất",
                    "Đã cập nhật " + patched + ".\n\n" +
                    "Bản gốc được lưu tại:\n" + ToAssetPath(backupDirectory) + "\n\n" +
                    "Chờ Unity compile xong, sau đó chọn ConveyorBeltRenderer > " +
                    "context menu > Rebuild Belt Mesh.",
                    "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Không thể cài đặt",
                    exception.Message + "\n\nKhông có file runtime nào được xóa.",
                    "OK");
                AssetDatabase.Refresh();
            }
        }

        [MenuItem("Tools/FruitSort/Conveyor Connection Fix/Restore Latest Backup", priority = 1201)]
        public static void RestoreLatestBackup()
        {
            string backupsAbsolute = ToAbsolutePath(BackupsRoot);
            if (!Directory.Exists(backupsAbsolute))
            {
                EditorUtility.DisplayDialog("Khôi phục", "Chưa có bản sao lưu.", "OK");
                return;
            }

            string latest = Directory.GetDirectories(backupsAbsolute)
                .OrderByDescending(path => path, StringComparer.Ordinal)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(latest))
            {
                EditorUtility.DisplayDialog("Khôi phục", "Chưa có bản sao lưu.", "OK");
                return;
            }

            string manifestPath = Path.Combine(latest, "manifest.txt");
            if (!File.Exists(manifestPath))
            {
                EditorUtility.DisplayDialog("Khôi phục", "Bản sao lưu thiếu manifest.txt.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Khôi phục bản gần nhất",
                    "Khôi phục nội dung script từ:\n" + ToAssetPath(latest) + "?",
                    "Khôi phục",
                    "Hủy"))
                return;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string line in File.ReadAllLines(manifestPath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] parts = line.Split(new[] { '|' }, 2);
                    if (parts.Length != 2) continue;

                    string targetAbsolute = ToAbsolutePath(parts[0]);
                    string backupAbsolute = Path.Combine(latest, parts[1]);
                    if (!File.Exists(backupAbsolute)) continue;
                    File.WriteAllText(targetAbsolute, File.ReadAllText(backupAbsolute), Utf8NoBom);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            EditorUtility.DisplayDialog("Khôi phục", "Đã khôi phục bản gần nhất.", "OK");
        }

        [MenuItem("Tools/FruitSort/Conveyor Connection Fix/Open Readme", priority = 1202)]
        public static void OpenReadme()
        {
            UnityEngine.Object readme = AssetDatabase.LoadAssetAtPath<TextAsset>(ReadmePath);
            if (readme == null)
            {
                EditorUtility.DisplayDialog("README", "Không tìm thấy " + ReadmePath, "OK");
                return;
            }

            Selection.activeObject = readme;
            EditorGUIUtility.PingObject(readme);
        }

        static string FindSingleTarget(string fileName, bool required)
        {
            string[] candidates = Directory.GetFiles(Application.dataPath, fileName, SearchOption.AllDirectories)
                .Where(path => path.IndexOf("/ConveyorConnectionFix/", StringComparison.OrdinalIgnoreCase) < 0 &&
                               path.IndexOf("\\ConveyorConnectionFix\\", StringComparison.OrdinalIgnoreCase) < 0)
                .ToArray();

            if (candidates.Length == 1) return candidates[0];

            if (candidates.Length == 0)
            {
                if (!required) return null;
                throw new FileNotFoundException(
                    "Không tìm thấy " + fileName + " trong thư mục Assets. " +
                    "Hãy bảo đảm script hiện tại đã nằm trong project.");
            }

            string list = string.Join("\n", candidates.Select(ToAssetPath));
            throw new InvalidOperationException(
                "Tìm thấy nhiều file " + fileName + ":\n" + list +
                "\n\nHãy giữ đúng một file rồi chạy Install or Update lại.");
        }

        static void ReplaceFromTemplate(string targetAbsolute, string templateAssetPath,
            string backupDirectory, string backupName, ICollection<string> manifest)
        {
            string templateAbsolute = ToAbsolutePath(templateAssetPath);
            if (!File.Exists(templateAbsolute))
                throw new FileNotFoundException("Không tìm thấy template: " + templateAssetPath);

            string targetAssetPath = ToAssetPath(targetAbsolute);
            string backupAbsolute = Path.Combine(backupDirectory, backupName);
            File.WriteAllText(backupAbsolute, File.ReadAllText(targetAbsolute), Utf8NoBom);
            manifest.Add(targetAssetPath + "|" + backupName);

            string replacement = File.ReadAllText(templateAbsolute);
            File.WriteAllText(targetAbsolute, replacement, Utf8NoBom);
            Debug.Log("[ConveyorConnectionFix] Updated " + targetAssetPath);
        }

        static string CreateBackupDirectory()
        {
            string rootAbsolute = ToAbsolutePath(BackupsRoot);
            Directory.CreateDirectory(rootAbsolute);

            string directory = Path.Combine(rootAbsolute, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            int suffix = 1;
            while (Directory.Exists(directory))
                directory = Path.Combine(rootAbsolute, DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + suffix++);

            Directory.CreateDirectory(directory);
            return directory;
        }

        static bool AssetExists(string assetPath)
        {
            return File.Exists(ToAbsolutePath(assetPath));
        }

        static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        static string ToAssetPath(string absolutePath)
        {
            string normalized = Path.GetFullPath(absolutePath).Replace('\\', '/');
            string assets = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
            if (!normalized.StartsWith(assets, StringComparison.OrdinalIgnoreCase)) return normalized;
            return "Assets" + normalized.Substring(assets.Length);
        }
    }
}
#endif
