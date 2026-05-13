using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Net.Tcp.State;
using UnityEngine;

namespace Net.Tcp {
    public sealed class TcpClientStateMachine {
        private readonly ITcpClientFSMCtx _context;

        private TcpClientStateBase _state;

        /// <summary>
        /// 当前所处状态的枚举身份。FSM 未启动 / 已退出时为 <see cref="ETcpState.None"/>。
        /// 外部代码用 <c>== ETcpState.Connected</c> 之类的判断阶段，不直接接触 state 类。
        /// </summary>
        public ETcpState CurrentState => _state?.Kind ?? ETcpState.None;

        /// <summary>
        /// 状态切换时触发，参数 <c>(prev, next)</c>。
        /// FSM 启动时 prev = <see cref="ETcpState.None"/>；FSM 退出（含异常 / 取消 / null 返回）时 next = <see cref="ETcpState.None"/>。
        /// 触发时机：在 prev 的 OnExit 之后、next 的 OnEnter 之前；即"prev 已退出，next 尚未进入"的过渡瞬间。
        /// </summary>
        public event Action<ETcpState, ETcpState> OnStateChanged;

        public TcpClientStateMachine(ITcpClientFSMCtx context) {
            _context = context;
        }

        public UniTask StartAsync<TInit>(CancellationToken token = default) where TInit : TcpClientStateBase {
            return StartAsync(TcpClientStateBase.GetInstance<TInit>(), token);
        }

        public async UniTask StartAsync(TcpClientStateBase initState, CancellationToken token = default) {
            SetState(initState);   // (None, initState.Kind)
            while (_state != null) {
                TcpClientStateBase next;
                try {
                    _state.OnEnterWrap(_context);
                    try {
                        next = await _state.RunAsync(_context, token);
                    } finally {
                        // OnExit 必须在退出（含异常）时执行，承担状态自身的清理职责
                        _state.OnExitWrap(_context);
                    }
                } catch (OperationCanceledException) {
                    // 取消是正常退出路径
                    SetState(null);   // (prev, None) 通知订阅方 FSM 退出
                    return;
                } catch (Exception e) {
                    Debug.LogErrorFormat("TcpClientState \"{0}\" run error.\n{1}", _state.GetType().Name, e);
                    SetState(null);   // (prev, None) 通知订阅方 FSM 因错误退出
                    return;
                }
                SetState(next);   // (prev, next.Kind) 正常转移；next 为 null 时也通知，循环条件会让 while 退出
            }
        }

        private void SetState(TcpClientStateBase next) {
            ETcpState prevKind = CurrentState;
            _state = next;
            ETcpState nextKind = CurrentState;
            if (prevKind != nextKind) {
                OnStateChanged?.Invoke(prevKind, nextKind);
            }
        }
    }
}
