using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Login {
    [Serializable]
    public sealed class ShowLoadingUIState : LoginStateBase {
        protected override async UniTask StateStart(CancellationToken token = default) {
            await UniTask.Yield();
        }
    }
}
