using System;
using System.Collections;
using UnityEngine;

namespace Game.Login {
    [Serializable]
    public sealed class AppStartState : LoginStateBase {
        protected override IEnumerator StateStart() {
            Debug.LogError("============ AppStartState: 初始化游戏...");
            yield return new WaitForEndOfFrame();
            SetNext<ShowAppLoadingUIState>();
        }
    }
}
