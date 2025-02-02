using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Editor {
    [CreateAssetMenu(fileName = "AssetBundleWindowData", menuName = "AssetBundle/AssetBundleWindowData")]
    public class AssetBundleWindowData : ScriptableObject {
        [FolderPath(RequireExistingPath = true)]
        [LabelText("Output path")]
        public string path = "AssetBundles";

        [LabelText("Options")]
        public BuildAssetBundleOptions buildAssetBundleOptions = BuildAssetBundleOptions.None;

        [LabelText("Build target")]
        [OnValueChanged(nameof(OnBuildTargetChanged))]
        public BuildTarget buildTarget = BuildTarget.StandaloneWindows64;

        [LabelText("Copy to StreamingAssets")]
        public bool copyToStreamingAssets = true;

        [LabelText("Open output folder")]
        public bool openFolderAfterBuild;

        [HideInInspector]
        public string streamingAssetsPath = "Assets/StreamingAssets";

        private void OnBuildTargetChanged() {
        }
    }
}
