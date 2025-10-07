using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Login {
    [Serializable]
    public sealed class AppStartState : LoginStateBase {
        protected override async UniTask StateStart(CancellationToken token = default) {
            Debug.LogError("============ AppStartState: 初始化游戏...");
            await UniTask.NextFrame();
            SetNext<ShowAppLoadingUIState>();
        }
    }
}
