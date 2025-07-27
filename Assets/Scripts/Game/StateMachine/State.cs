using System.Collections;

namespace Game.StateMachine {
    public abstract class State {
        public State NextState { get; protected set; }
        public abstract IEnumerator Start();
    }
}
