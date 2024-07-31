using UnityEditor;
using UnityEngine.Windows;

namespace Editor {
    public class AssetBundleTools {
        [MenuItem("AssetBundle/BuildAll")]
        public static void BuildAllAssetBundle() {
            try {
                AssetDatabase.StartAssetEditing();
                string path = "AssetBundles";
                FileUtil.DeleteFileOrDirectory(path);
                Directory.CreateDirectory(path);
                BuildPipeline.BuildAssetBundles(path, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
            } finally {
                AssetDatabase.StopAssetEditing();
            }
        }
    }
}