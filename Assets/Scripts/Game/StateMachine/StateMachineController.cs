using System;
using System.Collections;
using Sirenix.OdinInspector;

namespace Game.StateMachine {
    [Serializable]
    public class StateMachineController {
        [ShowInInspector]
        [LabelText("Current State")]
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
