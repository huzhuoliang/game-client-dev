using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Login {
    public sealed class AppStartState : LoginStateBase {
        public AppStartState(LoginStateMachine stateMachine) : base(stateMachine) {
        }

        public override IEnumerator Start() {
            Debug.LogError("============ AppStartState: 初始化游戏...");
            yield return new WaitForEndOfFrame();
            NextState = StateMachine.States.GetValueOrDefault(typeof(ShowAppLoadingUIState));
        }
    }
}
