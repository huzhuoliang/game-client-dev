using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Login {
    [Serializable]
    public sealed class ShowLoginUIState : LoginStateBase {
        protected override async UniTask StateStart(CancellationToken token = default) {
            Debug.LogError("============ ShowLoginUIState: 展示登录界面...");
            SetNext<ShowLoadingUIState>();
            await UniTask.Yield();
        }
    }
}
