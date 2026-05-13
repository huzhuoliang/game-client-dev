using System;
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

        /// <summary>当前 FSM 状态。FSM 未启动 / 已退出时为 <see cref="ETcpState.None"/>。</summary>
        public ETcpState CurrentState => _stateMachine?.CurrentState ?? ETcpState.None;

        /// <summary>简便属性：当前是否处于 <see cref="ETcpState.Connected"/>。</summary>
        public bool IsConnected => CurrentState == ETcpState.Connected;

        /// <summary>
        /// 状态切换事件，中转自引擎层。语义参见 <see cref="TcpClientStateMachine.OnStateChanged"/>：
        /// `(prev, next)`，FSM 启动 prev=None，FSM 退出 next=None。
        /// </summary>
        public event Action<ETcpState, ETcpState> OnStateChanged;

        private void Awake() {
            _ctx = new TcpClientFSMCtx("192.168.50.16", 50052);
            _stateMachine = new TcpClientStateMachine(_ctx);
            _stateMachine.OnStateChanged += OnEngineStateChanged;
        }

        private void OnEnable() {
            Connect();
        }

        private void OnDisable() {
            Disconnect();
        }

        private void OnDestroy() {
            // 兜底：FSM 可能还在退出途中（OnDisable 触发软中断但还没走完 Disconnecting）；强杀
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            if (_stateMachine != null) {
                _stateMachine.OnStateChanged -= OnEngineStateChanged;
                _stateMachine = null;
            }
            _ctx?.Dispose();
            _ctx = null;
            OnStateChanged = null;
        }

        /// <summary>
        /// 启动 FSM。幂等：FSM 已在跑时 no-op；已退出 / 未启动时新建 cts 并启动 Init。
        /// 注意：FSM 处于退出途中（如 Disconnecting）时也会被识别为"在跑"，
        /// 必须等其完成退出后下一次 Connect 才会真正起新 FSM。
        /// </summary>
        public void Connect() {
            if (_cts != null) {
                // FSM 已在跑（含正在退出途中）
                return;
            }
            _cts = new CancellationTokenSource();
            _stateMachine.StartAsync<Init>(_cts.Token).Forget();
        }

        /// <summary>
        /// 请求 FSM 走优雅退出路径（软中断）。
        /// 若处于 Connecting / Connected → 触发 OnDisconnectRequested → 状态机走到 Disconnecting → Disconnected → 退出。
        /// 若 FSM 未启动 / 已退出 → no-op（事件没人订阅）。
        /// 退出完成后 `_cts` 会被 <see cref="OnEngineStateChanged"/> 自动 dispose + 置 null，下次 Connect 可重新起。
        /// </summary>
        public void Disconnect() {
            _ctx?.RequestDisconnect();
        }

        private void OnEngineStateChanged(ETcpState prev, ETcpState next) {
            // FSM 退出（state 回到 None）时清理 cts，让下次 Connect 能起新 FSM
            if (next == ETcpState.None) {
                _cts?.Dispose();
                _cts = null;
            }
            OnStateChanged?.Invoke(prev, next);
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
