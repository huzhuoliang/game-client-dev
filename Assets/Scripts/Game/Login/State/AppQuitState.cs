using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Login {
    [Serializable]
    public sealed class AppQuitState : LoginStateBase {
        protected override async UniTask StateStart(CancellationToken token = default) {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
            await UniTask.Yield();
        }
    }
}
