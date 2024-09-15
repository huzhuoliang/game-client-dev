using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class ClosureTest : MonoBehaviour {
    [Button]
    private void Test() {
        int n = 3;
        TestCallback(() => { Debug.LogError($"n={n}"); });
    }

    private void TestCallback(Action cb) {
        cb?.Invoke();
    }
}
