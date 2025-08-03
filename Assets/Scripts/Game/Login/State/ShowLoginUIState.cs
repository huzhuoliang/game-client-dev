using System;
using System.Collections;
using UnityEngine;

namespace Game.Login {
    [Serializable]
    public sealed class ShowLoginUIState : LoginStateBase {
        protected override IEnumerator StateStart() {
            Debug.LogError("============ ShowLoginUIState: 展示登录界面...");
            SetNext<ShowLoadingUIState>();
            yield break;
        }
    }
}
