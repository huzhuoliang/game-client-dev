using System;
using Game.UI;
using Net.Mono;

namespace Game.Login {
    [Serializable]
    public class LoginStateMachineContext {
        public UIManager UIManager { get; private set; }
        public GameClient GameClient { get; private set; }

        public LoginStateMachineContext SetUIManager(UIManager uiManager) {
            UIManager = uiManager;
            return this;
        }

        public LoginStateMachineContext SetGameClient(GameClient gameClient) {
            GameClient = gameClient;
            return this;
        }
    }
}
