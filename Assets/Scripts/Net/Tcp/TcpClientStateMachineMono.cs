using System.Threading;
using Cysharp.Threading.Tasks;
using GameServerServices.HelloWorld;
using GameServerServices.MessageType;
using Net.Tcp.State;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Net.Tcp {
    public class TcpClientStateMachineMono : MonoBehaviour {
        private TcpClientFSMCtx _ctx;
        private TcpClientStateMachine _stateMachine;
        private CancellationTokenSource _cts;

        // UI / 外部调用方通过这个接口拿 SendMessage / OnConnectFailed / RequestDisconnect 等公共能力
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
            // 软中断：让 FSM 走 Connected → Disconnecting → Disconnected 干净退出
            // 不取消 ct：ct 取消是 FSM 紧急退出，不走清理路径
            _ctx?.RequestDisconnect();
        }

        private void OnDestroy() {
            // 兜底：FSM 还没退出就强杀；同时确保 ctx 资源释放
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _ctx?.Dispose();
            _ctx = null;
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
