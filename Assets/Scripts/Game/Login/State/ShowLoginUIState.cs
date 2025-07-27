using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Login {
    public sealed class ShowLoginUIState : LoginStateBase {
        public ShowLoginUIState(LoginStateMachine stateMachine) : base(stateMachine) {
        }

        public override IEnumerator Start() {
            Debug.LogError("============ ShowLoginUIState: 展示登录界面...");
            NextState = StateMachine.States.GetValueOrDefault(typeof(ShowLoadingUIState));
            yield break;
        }
    }
}
