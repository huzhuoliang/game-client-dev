using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TestScript : MonoBehaviour {
    [Button]
    private void Test() {
        var c = new Color(30f / 255f, 132f / 255f, 73f / 255f);
    }
}
