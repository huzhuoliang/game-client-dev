using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesTestLoader : MonoBehaviour {
    [SerializeField]
    private AssetReference assetReference;

    [Button]
    [DisableInEditorMode]
    private void LoadAsset() {
        AsyncOperationHandle<GameObject> handle = assetReference.LoadAssetAsync<GameObject>();
        handle.Completed += HandleOnCompleted;
    }

    private void HandleOnCompleted(AsyncOperationHandle<GameObject> handle) {
        GameObject asset = handle.Result;
        GameObject obj = Instantiate(asset, transform);
        Debug.LogError($"加载完毕 {obj.name}");
                
    }
}