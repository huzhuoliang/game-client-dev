using System.IO;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Device.Application;
using Directory = System.IO.Directory;
using File = System.IO.File;
using Game;

namespace Editor {
    public class AssetBundleWindow : OdinEditorWindow {
        [SerializeField]
        [InlineEditor(InlineEditorModes.GUIOnly, InlineEditorObjectFieldModes.Hidden)]
        [PropertySpace(0f, 20f)]
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

        [Button("Build", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        [PropertySpace(20f, 20f)]
        [PropertyOrder(10000)]
        [DisableInPlayMode]
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

        [Button("Open Output Path")]
        private void OpenOutputPath() {
            string outputPath = Path.Join(data.path, data.buildTarget.ToString());
            if (!outputPath.EndsWith("\\"))
                outputPath += "\\";
            if (Directory.Exists(outputPath)) {
                EditorUtility.RevealInFinder(outputPath);
            }
        }

        private void BuildAB() {
            string outputPath = Path.Join(data.path, data.buildTarget.ToString());
            FileUtil.DeleteFileOrDirectory(outputPath);
            Directory.CreateDirectory(outputPath);
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(outputPath, data.buildAssetBundleOptions, data.buildTarget);

            if (manifest == null) {
                Debug.LogError("Failed to build AssetBundle.");
                return;
            }

            CreateAssetBundleInfoJson(manifest, outputPath);

            if (data.copyToStreamingAssets) {
                CopyFolderToStreamingAssets(data.path);
            }

            string revealPath = outputPath;
            if (!revealPath.EndsWith("\\"))
                revealPath += "\\";
            if (data.openFolderAfterBuild) {
                EditorUtility.RevealInFinder(revealPath);
            }

            string fullOutputPath = Path.GetFullPath(revealPath);
            Debug.Log($"AssetBundle build success.\nPath: \"{fullOutputPath}\"\n");
        }

        /// <summary>
        /// Write AssetBundle meta data to Json file
        /// </summary>
        /// <param name="manifest"></param>
        /// <param name="outputPath">The path where the AssetBundle is saved，and also the path where the JSON file is output.</param>
        /// <returns></returns>
        private static void CreateAssetBundleInfoJson(AssetBundleManifest manifest, string outputPath) {
            if (manifest == null) {
                return;
            }

            AssetBundleInfos infos = new();
            foreach (string assetBundle in manifest.GetAllAssetBundles()) {
                string bundlePath = Path.Combine(outputPath, assetBundle);
                ulong bytes = GetFileSizeBytes(bundlePath);
                Hash128 hash = manifest.GetAssetBundleHash(assetBundle);
                if (!BuildPipeline.GetCRCForAssetBundle(bundlePath, out uint crc)) {
                    Debug.Log($"AssetBundle \"{assetBundle}\" Get CRC Error.");
                    continue;
                }

                infos.InfoList.Add(new AssetBundleInfoUnit {
                        BundleName = assetBundle,
                        CRC = crc,
                        Hash = hash.ToString(),
                        Bytes = bytes,
                });
            }

            string json = JsonUtility.ToJson(infos, true);
            string infoPath = Path.Combine(outputPath, MainABUnit.AssetBundleInfoFileName);
            File.WriteAllText(infoPath, json);
        }

        private static ulong GetFileSizeBytes(string path) {
            ulong size = 0;
            FileInfo fileInfo = new FileInfo(path);
            if (fileInfo.Exists) {
                size = fileInfo.Length >= 0 ? (ulong)fileInfo.Length : 0;
            }

            return size;
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
