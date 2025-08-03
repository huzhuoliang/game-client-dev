using System;
using System.Collections;

namespace Game.StateMachine {
    [Serializable]
    public abstract class State {
        public State NextState { get; protected set; }
        public abstract IEnumerator Start();
    }
}
