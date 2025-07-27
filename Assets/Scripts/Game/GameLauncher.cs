using Game.Login;
using Game.UI;
using Net.Mono;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game {
    /// <summary>
    /// 游戏启动流程
    /// </summary>
    [RequireComponent(typeof(LoginStateMachine))]
    public class GameLauncher : MonoBehaviour {
        [SerializeField]
        [Required]
        private GameClient gameClient;

        [SerializeField]
        [Required]
        private UIManager uiManager;

        private LoginStateMachine _stateMachine;

        private void Awake() {
            _stateMachine = GetComponent<LoginStateMachine>();
        }

        private void Start() {
            _stateMachine
                    .SetUIManager(uiManager)
                    .SetGameClient(gameClient)
                    .StartStateMachine();
        }
    }
}
