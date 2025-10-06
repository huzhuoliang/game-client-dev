using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.StateMachine {
    [Serializable]
    public class StateMachineController {
        [ShowInInspector]
        [LabelText("Current State")]
        public State CurrState { get; private set; }

        public async UniTask Start(State initialState) {
            if (initialState == null) {
                return;
            }

            while (initialState != null) {
                CurrState = initialState;

                try {
                    await initialState.Start();
                } catch (Exception e) {
                    Debug.LogException(e);
                }

                // 捕获协程异常并输出，避免某个状态异常以后整个状态机停止
                initialState = initialState.GetNext();
            }
        }
    }
}
