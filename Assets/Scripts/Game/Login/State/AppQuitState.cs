using System;
using System.Collections;

namespace Game.Login {
    [Serializable]
    public class AppQuitState : LoginStateBase {
        public AppQuitState() {
        }

        public AppQuitState(LoginStateMachine stateMachine) : base(stateMachine) {
        }

        protected override IEnumerator StateStart() {
            NextState = null;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
            yield break;
        }
    }
}
