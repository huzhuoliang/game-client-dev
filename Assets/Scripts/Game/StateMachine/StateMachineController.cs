using System.Collections;

namespace Game.StateMachine {
    public class StateMachineController {
        public State CurrState { get; private set; }

        public IEnumerator Start(State state) {
            if (state == null) {
                yield break;
            }

            while (state != null) {
                CurrState = state;
                yield return state.Start();
                state = state.NextState;
            }
        }
    }
}
