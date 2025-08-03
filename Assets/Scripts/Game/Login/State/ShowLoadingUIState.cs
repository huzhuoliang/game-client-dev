using System;
using System.Collections;

namespace Game.Login {
    [Serializable]
    public sealed class ShowLoadingUIState : LoginStateBase {
        public ShowLoadingUIState() {
        }

        public ShowLoadingUIState(LoginStateMachine stateMachine) : base(stateMachine) {
        }

        protected override IEnumerator StateStart() {
            NextState = null;
            yield break;
        }
    }
}
