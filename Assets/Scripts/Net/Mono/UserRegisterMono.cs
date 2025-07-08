using GameServerServices.MessageType;
using GameServerServices.User.Register;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Net.Mono {
    public class UserRegisterMono : MonoBehaviour {
        [SerializeField]
        [Required]
        private GameClient gameClient;

        [SerializeField]
        private string username;

        [SerializeField]
        private string password;

        [Button]
        private void Send() {
            if (!gameClient.TcpClient.Connected) {
                Debug.LogErrorFormat("Not connected");
                return;
            }

            UserRegisterReq request = new UserRegisterReq {
                    Username = username,
                    Password = password,
            };
            gameClient.TcpClient.SendMessage(MessageType.MsgUserRegisterReq, request);
        }
    }
}
