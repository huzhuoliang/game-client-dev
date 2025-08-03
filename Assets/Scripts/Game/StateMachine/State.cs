using System;
using System.Collections;

namespace Game.StateMachine {
    [Serializable]
    public abstract class State {
        private State _nextState;
        public abstract IEnumerator Start();

        protected void SetNext(State state) {
            _nextState = state;
        }

        internal State GetNext() {
            return _nextState;
        }
    }
}
