using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Test {
    public class UniTaskTest : MonoBehaviour {
        [Button]
        private void Test() {
            StartWorkAsync().Forget();
        }

        private async UniTask StartWorkAsync() {
            Debug.LogErrorFormat("Work Start");
            string work = await WorkAsync();
            Debug.LogErrorFormat("str={0}", work);
            Debug.LogErrorFormat("Work End");
        }

        private async UniTask<string> WorkAsync() {
            await UniTask.Delay(1000);
            return "success";
        }
    }
}
