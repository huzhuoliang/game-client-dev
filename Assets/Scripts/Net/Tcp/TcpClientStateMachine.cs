using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Net.Proto.State;
using UnityEngine;

namespace Net.Proto {
    public sealed class TcpClientStateMachine {
        private readonly ITcpClientFSMCtx _context;

        private TcpClientStateBase _state;

        public TcpClientStateMachine(ITcpClientFSMCtx context) {
            _context = context;
        }

        public UniTask StartAsync<TInit>(CancellationToken token = default) where TInit : TcpClientStateBase {
            return StartAsync(TcpClientStateBase.GetInstance<TInit>(), token);
        }

        public async UniTask StartAsync(TcpClientStateBase initState, CancellationToken token = default) {
            _state = initState;
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
                    return;
                } catch (Exception e) {
                    Debug.LogErrorFormat("TcpClientState \"{0}\" run error.\n{1}", _state.GetType().Name, e);
                    return;
                }
                _state = next;
            }
        }
    }
}
