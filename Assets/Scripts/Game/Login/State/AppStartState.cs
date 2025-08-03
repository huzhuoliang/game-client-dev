using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Login {
    [Serializable]
    public sealed class AppStartState : LoginStateBase {
        public AppStartState() {
        }

        public AppStartState(LoginStateMachine stateMachine) : base(stateMachine) {
        }

        protected override IEnumerator StateStart() {
            Debug.LogError("============ AppStartState: 初始化游戏...");
            yield return new WaitForEndOfFrame();
            NextState = StateMachine.States.GetValueOrDefault(typeof(ShowAppLoadingUIState));
        }
    }
}
