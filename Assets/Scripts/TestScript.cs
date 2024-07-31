using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class TestScript : MonoBehaviour {
    [Button]
    private void Test() {
        using IEnumerator<int> enumerator = EnumerableFunc().GetEnumerator();
        while (enumerator.MoveNext()) {
            int current = enumerator.Current;
            Debug.LogError($"current={current}");
        }
    }

    private static IEnumerable<int> EnumerableFunc() {
        yield return 1;
        yield return 2;
        yield return 3;
    }
}