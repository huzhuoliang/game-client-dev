using System;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

public class TestGameObject : MonoBehaviour {
    [Button]
    private void LoadObj() {
        string path = Path.Combine(Application.streamingAssetsPath, "StandaloneWindows64/test_bundle/test1");
        Debug.LogFormat("path={0}", path);
        try {
            AssetBundle ab = AssetBundle.LoadFromFile(path);

            //        var prefab = myLoadAssetBundle.LoadAsset<GameObject>("Sphere");
            //        GameObject obj = Instantiate(prefab);
            //        obj.name = "Sphere";
            //        prefab = myLoadAssetBundle.LoadAsset<GameObject>("Sphere2");
            //        obj = Instantiate(prefab);
            //        obj.name = "Sphere2";

            //        var prefab = myLoadAssetBundle.LoadAsset<GameObject>("assets/bundle/test/prefab/sphere.prefab");
            //        var obj = Instantiate(prefab);
            //        obj.name = "Sphere";

            string[] names = ab.GetAllAssetNames();
            Debug.LogFormat("names={0}", string.Join(",", names));
        } catch (Exception e) {
            Console.WriteLine(e);
            throw;
        }
    }

    [Button]
    private void LoadManifect() {
        string path = Path.Combine(Application.streamingAssetsPath, "StandaloneWindows64/StandaloneWindows64");
        AssetBundle abMain = AssetBundle.LoadFromFile(path);
        AssetBundleManifest manifest = abMain.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        string[] dependencies = manifest.GetAllDependencies("test_bundle/test1");
        Debug.LogFormat("依赖信息：{{{0}}}", string.Join(",", dependencies));
    }

    [Button]
    private void ShowAllLoadedAssetBundles() {
        foreach (AssetBundle assetBundle in AssetBundle.GetAllLoadedAssetBundles()) {
            Debug.LogError(assetBundle.name);
        }
    }
}