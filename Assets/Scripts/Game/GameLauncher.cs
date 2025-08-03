using Game.Login;
using Game.UI;
using Net.Mono;
using UnityEngine;

namespace Game {
    /// <summary>
    /// 游戏启动流程
    /// </summary>
    [RequireComponent(typeof(GameClient))]
    [RequireComponent(typeof(UIManager))]
    [RequireComponent(typeof(LoginStateMachineMono))]
    public class GameLauncher : MonoBehaviour {
        private GameClient _gameClient;

        private UIManager _uiManager;

        private LoginStateMachineMono _stateMachine;

        private void Awake() {
            _gameClient = GetComponent<GameClient>();
            _uiManager = GetComponent<UIManager>();
            _stateMachine = GetComponent<LoginStateMachineMono>();
        }

        private void Start() {
            // Init
            _stateMachine.StateMachine.Init();
            _stateMachine.StateMachine.Context
                    .SetUIManager(_uiManager)
                    .SetGameClient(_gameClient);
            // Start
            StartCoroutine(_stateMachine.StateMachine.StartStateMachine());
        }
    }
}
