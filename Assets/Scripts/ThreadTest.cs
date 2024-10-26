using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class ThreadTest : MonoBehaviour {
    private static int logNumber = 0;

    private void Start() {
        Application.logMessageReceived -= LogMessageReceived;
        Application.logMessageReceived += LogMessageReceived;
    }

    private void OnValidate() {
        Application.logMessageReceived -= LogMessageReceived;
        Application.logMessageReceived += LogMessageReceived;
    }

    private static void LogMessageReceived(string msg1, string msg2, LogType logType) {
        logNumber++;
    }

    [Button]
    private void Test() {
        Debug.LogError($"Number={logNumber}");
    }
}
