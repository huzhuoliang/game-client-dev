using System;
using DefaultNamespace;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TestScript : MonoBehaviour {
    private void Awake() {
        gameObject.AddComponent<TestComponent>();
    }
}
