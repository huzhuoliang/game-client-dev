using System.Collections;
using Game.UI;

namespace Game.Login {
    public sealed class ShowLoadingUIState : LoginStateBase {
        public ShowLoadingUIState(LoginStateMachine stateMachine) : base(stateMachine) {
        }

        public override IEnumerator Start() {
            NextState = null;
            yield break;
        }
    }
}
