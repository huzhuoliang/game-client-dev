using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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

        public sealed override async UniTask Start(CancellationToken token = default) {
            if (StateMachine == null) {
                return;
            }

            await StateStart(token);
        }

        protected void SetNext<T>() where T : LoginStateBase {
            if (StateMachine == null) {
                return;
            }

            SetNext(StateMachine.GetState<T>());
        }

        protected abstract UniTask StateStart(CancellationToken token = default);
    }
}
