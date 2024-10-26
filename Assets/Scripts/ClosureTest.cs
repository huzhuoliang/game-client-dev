using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Threading.Tasks;

public class ClosureTest : MonoBehaviour {
    [Button]
    private void Test() {
        Task.Run(() => {
            Debug.LogError("Start");
            List<Task> tasks = new();
            for (int i = 5; i < 10; i++) {
                tasks.Add(WaitSeconds(i));
            }

            Task.WaitAll(tasks.ToArray());
            Debug.LogError("End");
        });
    }

    private static async Task WaitSeconds(float sec) {
        Debug.LogError($"Wait {sec:0.00}s");
        await Task.Delay(Mathf.FloorToInt(sec * 1000f));
        Debug.LogError($"Wait {sec:0.00}s done");
    }
}
