using System.Collections;
using Net.Mono;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game {
    /// <summary>
    /// 游戏启动流程
    /// </summary>
    public class GameLauncher : MonoBehaviour {
        [SerializeField]
        [Required]
        private GameClient gameClient;

        private IEnumerator Start() {
            gameClient.Connect();
            yield break;
        }
    }
}
