using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.StateMachine {
    [Serializable]
    public abstract class State {
        private State _nextState;

        public abstract UniTask Start(CancellationToken token = default);

        protected void SetNext(State state) {
            _nextState = state;
        }

        internal State GetNext() {
            return _nextState;
        }
    }
}
