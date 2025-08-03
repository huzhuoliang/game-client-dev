using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using Game.CoroutineExtensions;

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
                IEnumerator routine = state.Start();
                routine = CoroutineExtension.WrapCoroutine(routine, Debug.LogException);
                yield return routine;
                state = state.NextState;
            }
        }
    }
}
