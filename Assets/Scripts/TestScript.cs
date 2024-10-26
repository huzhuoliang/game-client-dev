using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TestScript : MonoBehaviour {
    [Button]
    private void Test() {
#if UNITY_EDITOR
        string[] bundleNames = AssetDatabase.GetAllAssetBundleNames();
        Debug.LogError($"Bundle name number is {bundleNames.Length} :\n {string.Join(",\n", bundleNames)}");
#endif
    }
}
