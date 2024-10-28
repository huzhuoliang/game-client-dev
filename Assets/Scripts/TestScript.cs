using System;
using System.Collections;
using Game;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TestScript : MonoBehaviour {
    private IEnumerator Start() {
        Coroutine<int> coroutine = this.StartCoroutine<int>(COTest());
        yield return coroutine.unityCoroutine;
        try {
            int v = coroutine.Result;
            Debug.LogError($"协程结果是: {v}");
        } catch (Exception e) {
            Debug.LogError($"协程发生异常:\n{e}");
            // Debug.Break();
        }
    }

    [Button]
    private void Test() {
    }


    private IEnumerator COTest() {
        Debug.LogError("协程 步骤 1");
        yield return null;
        Debug.LogError("协程 步骤 2");
        yield return null;
        Debug.LogError("协程 步骤 3");
        throw new Exception("haha Exception");
    }
}
