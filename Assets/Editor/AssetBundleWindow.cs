using System.IO;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Device.Application;
using Directory = System.IO.Directory;
using File = System.IO.File;

namespace Editor {
    public class AssetBundleWindow : OdinEditorWindow {
        [SerializeField]
        [InlineEditor(InlineEditorModes.GUIOnly, InlineEditorObjectFieldModes.Hidden)]
        private AssetBundleWindowData data;

        private const string DataPath = "Assets/Editor/";
        private const string DataName = "AssetBundleWindowData.asset";
        private const string DataFullPath = DataPath + DataName;

        private void RefreshData() {
            if (File.Exists(DataFullPath)) {
                data = AssetDatabase.LoadAssetAtPath<AssetBundleWindowData>(DataFullPath);
            } else {
                Debug.LogError("创建数据");
                data = CreateAssetBundleWindowData();
            }
        }

        private static AssetBundleWindowData CreateAssetBundleWindowData() {
            AssetBundleWindowData data = CreateInstance<AssetBundleWindowData>();
            if (!Directory.Exists(DataPath)) {
                Directory.CreateDirectory(DataPath);
            }

            bool fileExists = File.Exists(DataFullPath);
            bool isOverrideFile = false;
            if (fileExists) {
                isOverrideFile = EditorUtility.DisplayDialog("文件已存在",
                        $"{DataFullPath}\n文件已存在，是否覆盖？", "是", "否");
            }

            if (fileExists && isOverrideFile) {
                AssetDatabase.DeleteAsset(DataFullPath);
            }

            if (!fileExists || isOverrideFile) {
                AssetDatabase.CreateAsset(data, DataFullPath);
                AssetDatabase.Refresh();
            }

            data = AssetDatabase.LoadAssetAtPath<AssetBundleWindowData>(DataFullPath);
            return data;
        }

        [MenuItem("AssetBundle/Window")]
        private static void OpenWindow() {
            AssetBundleWindow window = GetWindow<AssetBundleWindow>();
            window.RefreshData();
            window.Show();
        }

        [Button("Build")]
        private void Build() {
            try {
                AssetDatabase.StartAssetEditing();
                BuildAB();
            } finally {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        [Button("Clear StreamingAssets")]
        private void ClearStreamingAssetsFolder() {
            string path = Application.streamingAssetsPath;
            try {
                AssetDatabase.StartAssetEditing();
                if (AssetDatabase.IsValidFolder(path))
                    FileUtil.DeleteFileOrDirectory(path);
                Directory.CreateDirectory(path);
            } finally {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        [Button("Clear AssetBundles")]
        private void ClearAssetBundlesFolder() {
            string path = data.path;
            FileUtil.DeleteFileOrDirectory(path);
            Directory.CreateDirectory(path);
        }

        private void BuildAB() {
            string outputPath = Path.Join(data.path, data.buildTarget.ToString());
            FileUtil.DeleteFileOrDirectory(outputPath);
            Directory.CreateDirectory(outputPath);
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(outputPath, data.buildAssetBundleOptions, data.buildTarget);
            string[] bundles = manifest.GetAllAssetBundles();
            Debug.LogErrorFormat("打包结果：\n{0}", string.Join(",\n", bundles));

            if (data.copyToStreamingAssets)
                CopyFolderToStreamingAssets(data.path);

            string revealPath = outputPath;
            if (!revealPath.EndsWith("\\"))
                revealPath += "\\";
            if (data.openFolderAfterBuild)
                EditorUtility.RevealInFinder(revealPath);

            string fullOutputPath = Path.GetFullPath(revealPath);
            Debug.Log($"AssetBundle build success. Path = \"{fullOutputPath}\"");
        }

        private static void CopyFolderToStreamingAssets(string path) {
            if (!Directory.Exists(path)) {
                string fullPath = Path.GetFullPath(path);
                Debug.LogError($"Path not found. Path = \"{fullPath})\"");
                return;
            }

            FileUtil.DeleteFileOrDirectory(Application.streamingAssetsPath);
            FileUtil.CopyFileOrDirectory(path, Application.streamingAssetsPath);
        }
    }
}
