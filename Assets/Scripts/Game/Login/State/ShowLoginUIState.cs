using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Login {
    [Serializable]
    public sealed class ShowLoginUIState : LoginStateBase {
        protected override async UniTask StateStart() {
            Debug.LogError("============ ShowLoginUIState: 展示登录界面...");
            SetNext<ShowLoadingUIState>();
            await UniTask.Yield();
        }
    }
}
