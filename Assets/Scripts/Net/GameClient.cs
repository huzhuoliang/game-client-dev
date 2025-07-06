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

        // TODO
        [TextArea]
        [SerializeField]
        [HideLabel]
        private string msg;

        private readonly GameTcpClient _tcpClient = new();

        private void Start() {
            _tcpClient.SetAddr(addr).SetPort(port).Init();
            _ = _tcpClient.StartConnectionAsync();
        }

        private void OnDestroy() {
            _tcpClient?.Close();
        }

        [Button]
        [DisableInEditorMode]
        private void Test() {
            if (!_tcpClient.Connected) {
                return;
            }

            HelloRequest request = new HelloRequest { Name = "Unity Player" };
            _tcpClient.SendMessage(1, request);
        }
    }
}
