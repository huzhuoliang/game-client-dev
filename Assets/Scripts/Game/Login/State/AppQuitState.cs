using System;
using System.Collections;

namespace Game.Login {
    [Serializable]
    public sealed class AppQuitState : LoginStateBase {
        protected override IEnumerator StateStart() {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
            yield break;
        }
    }
}
