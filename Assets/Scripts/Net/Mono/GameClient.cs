using GameServerServices.HelloWorld;
using GameServerServices.MessageType;
using Net.Proto;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Net.Mono {
    public class GameClient : MonoBehaviour {
        [SerializeField]
        private string addr = "192.168.50.16";

        [SerializeField]
        private int port = 50052;

        [ShowInInspector]
        private bool IsClientStart => TcpClient.IsStart;

        [ShowInInspector]
        private bool IsClientConnected => TcpClient.Connected;

        public GameTcpClient TcpClient { get; } = new();

        private void Start() {
            TcpClient.OnClose += OnClose;
        }


        [HorizontalGroup("Conn")]
        [Button(ButtonSizes.Large)]
        [EnableIf("@!this.TcpClient.IsStart")]
        [GUIColor(0.5f, 1.0f, 0.5f)]
        private void Connect() {
            if (TcpClient.Connected) {
                return;
            }

            _ = TcpClient.StartConnectionAsync(addr, port);
        }

        [HorizontalGroup("Conn")]
        [Button(ButtonSizes.Large)]
        [EnableIf("@this.TcpClient.IsStart")]
        [GUIColor(1.0f, 0.5f, 0.5f)]
        private void Disconnect() {
            _ = TcpClient.Close();
        }

        private void OnDestroy() {
            _ = TcpClient.Close();
        }

        [PropertySpace(SpaceBefore = 20f)]
        [Button]
        [DisableInEditorMode]
        [EnableIf("@this.TcpClient.Connected")]
        private void Test() {
            if (!TcpClient.Connected) {
                return;
            }

            HelloRequest request = new HelloRequest { Name = "Unity Player" };
            TcpClient.SendMessage(MessageType.MsgHelloworldRequest, request);
        }

        private static void OnClose() {
        }
    }
}
