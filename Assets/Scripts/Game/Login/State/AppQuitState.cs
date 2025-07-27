using System.Collections;
using UnityEngine;

namespace Game.Login {
    public class AppQuitState : LoginStateBase {
        public AppQuitState(LoginStateMachine stateMachine) : base(stateMachine) {
        }

        public override IEnumerator Start() {
            NextState = null;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            yield break;
        }
    }
}
