using System;
using System.Collections;

namespace Game.Login {
    [Serializable]
    public sealed class ShowLoadingUIState : LoginStateBase {
        protected override IEnumerator StateStart() {
            yield break;
        }
    }
}
