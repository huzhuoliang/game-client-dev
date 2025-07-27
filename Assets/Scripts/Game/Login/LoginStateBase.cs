using System.Collections;
using Game.StateMachine;

namespace Game.Login {
    public abstract class LoginStateBase : State {
        protected readonly LoginStateMachine StateMachine;

        protected LoginStateBase(LoginStateMachine stateMachine) {
            StateMachine = stateMachine;
        }

        public abstract override IEnumerator Start();
    }
}
