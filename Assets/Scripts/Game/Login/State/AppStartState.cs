using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Login {
    [Serializable]
    public sealed class AppStartState : LoginStateBase {
        protected override async UniTask StateStart() {
            Debug.LogError("============ AppStartState: 初始化游戏...");
            await UniTask.NextFrame();
            SetNext<ShowAppLoadingUIState>();
        }
    }
}
