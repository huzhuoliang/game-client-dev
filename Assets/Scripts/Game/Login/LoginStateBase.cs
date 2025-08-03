using System;
using System.Collections;
using Game.StateMachine;

namespace Game.Login {
    [Serializable]
    public abstract class LoginStateBase : State {
        protected LoginStateMachine StateMachine { get; private set; }

        /// <summary>
        /// 无参构造仅用于测试，没有正确赋值状态机的状态无法运行
        /// </summary>
        protected LoginStateBase() {
            StateMachine = null;
        }

        public void Init(LoginStateMachine stateMachine) {
            StateMachine = stateMachine;
        }

        public override IEnumerator Start() {
            if (StateMachine == null) {
                yield break;
            }

            yield return StateStart();
        }

        protected void SetNext<T>() where T : LoginStateBase {
            if (StateMachine == null) {
                return;
            }

            SetNext(StateMachine.GetState<T>());
        }

        protected abstract IEnumerator StateStart();
    }
}
