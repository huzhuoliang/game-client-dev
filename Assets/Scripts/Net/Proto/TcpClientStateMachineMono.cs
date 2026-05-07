using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Util;
using GameServerServices.HelloWorld;
using GameServerServices.MessageType;
using Google.Protobuf;
using Net.Proto.State;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Net.Proto {
    public class TcpClientStateMachineMono : MonoBehaviour {
        private TcpClientFSMCtx _ctx;
        private TcpClientStateMachine _stateMachine;
        private CancellationTokenSource _cts;

        private void Awake() {
            _ctx = new TcpClientFSMCtx("192.168.50.16", 50052);
            _stateMachine = new TcpClientStateMachine(_ctx);
        }

        private void OnEnable() {
            _cts = new CancellationTokenSource();
            _stateMachine.StartAsync<Init>(_cts.Token).Forget();
        }

        private void OnDisable() {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        // 临时调试入口：等 #5 公共 SendMessage API 落地后替换掉
        [PropertySpace(SpaceBefore = 20f)]
        [Button("Send HelloWorld")]
        [DisableInEditorMode]
        private void DebugSendHelloWorld() {
            HelloRequest request = new HelloRequest { Name = "Unity Player (FSM)" };
            DebugSend(MessageType.MsgHelloworldRequest, request);
        }

        private void DebugSend(MessageType messageType, IMessage message) {
            Stream stream = _ctx?.NetworkStream;
            if (stream == null) {
                Debug.LogWarning("Stream not ready — connect first");
                return;
            }

            const uint MAGIC = 0xCAFEBABE;
            byte[] body = message.ToByteArray();
            uint crc = Crc32.Compute(body, 0, body.Length);

            using MemoryStream mem = new MemoryStream();
            mem.Write(MAGIC.GetBytesBigEndian(), 0, 4);
            mem.Write(((ushort)messageType).GetBytesBigEndian(), 0, 2);
            mem.Write(((uint)body.Length).GetBytesBigEndian(), 0, 4);
            mem.Write(body, 0, body.Length);
            mem.Write(crc.GetBytesBigEndian(), 0, 4);
            byte[] payload = mem.ToArray();
            stream.Write(payload, 0, payload.Length);
        }
    }
}
