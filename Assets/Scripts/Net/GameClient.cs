using GameServerServices.HelloWorld;
using Net.Proto;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Net {
    public class GameClient : MonoBehaviour {
        [SerializeField]
        private string addr = "192.168.50.16";

        [SerializeField]
        private int port = 50052;

        [ShowInInspector]
        private bool IsClientStart => _tcpClient.IsStart;

        [ShowInInspector]
        private bool IsClientConnected => _tcpClient.Connected;

        private readonly GameTcpClient _tcpClient = new();

        private void Start() {
            _tcpClient.OnClose += OnClose;
        }


        [HorizontalGroup("Conn")]
        [Button(ButtonSizes.Large)]
        [EnableIf("@!this._tcpClient.IsStart")]
        [GUIColor(0.5f, 1.0f, 0.5f)]
        private void Connect() {
            if (_tcpClient.Connected) {
                return;
            }

            _ = _tcpClient.StartConnectionAsync(addr, port);
        }

        [HorizontalGroup("Conn")]
        [Button(ButtonSizes.Large)]
        [EnableIf("@this._tcpClient.IsStart")]
        [GUIColor(1.0f, 0.5f, 0.5f)]
        private void Disconnect() {
            _tcpClient.Close();
        }

        private void OnDestroy() {
            _tcpClient?.Close();
        }

        [PropertySpace(SpaceBefore = 20f)]
        [Button]
        [DisableInEditorMode]
        [EnableIf("@this._tcpClient.Connected")]
        private void Test() {
            if (!_tcpClient.Connected) {
                return;
            }

            HelloRequest request = new HelloRequest { Name = "Unity Player" };
            _tcpClient.SendMessage(1, request);
        }

        private static void OnClose() {
        }
    }
}
