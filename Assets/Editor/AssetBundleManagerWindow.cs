using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Editor {
    public class AssetBundleManagerWindow : OdinEditorWindow {
        [SerializeField]
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = false)]
        private List<AssetBundleManagerInfo> infoList = new();

        [MenuItem("AssetBundle/ManagerWindow")]
        private static void OpenWindow() {
            AssetBundleManagerWindow window = GetWindow<AssetBundleManagerWindow>();
            window.Show();
        }

        [Button("刷新")]
        private void RefreshInfoList() {
            infoList.Clear();
            foreach (AssetBundle assetBundle in AssetBundle.GetAllLoadedAssetBundles()) {
                infoList.Add(new AssetBundleManagerInfo(assetBundle));
            }
        }

        [Button("卸载所有AssetBundles")]
        private void UnloadAll() {
            AssetBundle.UnloadAllAssetBundles(true);
            infoList.Clear();
        }
    }
}