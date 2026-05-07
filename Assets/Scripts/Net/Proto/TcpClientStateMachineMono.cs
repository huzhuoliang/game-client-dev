using System.Threading;
using Cysharp.Threading.Tasks;
using GameServerServices.HelloWorld;
using GameServerServices.MessageType;
using Net.Proto.State;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Net.Proto {
    public class TcpClientStateMachineMono : MonoBehaviour {
        private TcpClientFSMCtx _ctx;
        private TcpClientStateMachine _stateMachine;
        private CancellationTokenSource _cts;

        // UI / 外部调用方通过这个接口拿 SendMessage / OnConnectFailed 等公共能力
        public ITcpClientFSMCtx Context => _ctx;

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

        // 调试入口：发送 HelloWorld，期望 TcpHelloWorldHandler 收到 reply 并打日志
        [PropertySpace(SpaceBefore = 20f)]
        [Button("Send HelloWorld")]
        [DisableInEditorMode]
        private void DebugSendHelloWorld() {
            HelloRequest request = new HelloRequest { Name = "Unity Player (FSM)" };
            _ctx.SendMessage(MessageType.MsgHelloworldRequest, request).Forget();
        }
    }
}
