using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Login {
    [Serializable]
    public sealed class ShowLoginUIState : LoginStateBase {
        public ShowLoginUIState() {
        }

        public ShowLoginUIState(LoginStateMachine stateMachine) : base(stateMachine) {
        }

        protected override IEnumerator StateStart() {
            Debug.LogError("============ ShowLoginUIState: 展示登录界面...");
            NextState = StateMachine.States.GetValueOrDefault(typeof(ShowLoadingUIState));
            yield break;
        }
    }
}
