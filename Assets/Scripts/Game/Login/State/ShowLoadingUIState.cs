using System;
using Cysharp.Threading.Tasks;

namespace Game.Login {
    [Serializable]
    public sealed class ShowLoadingUIState : LoginStateBase {
        protected override async UniTask StateStart() {
            await UniTask.Yield();
        }
    }
}
