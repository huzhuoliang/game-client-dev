using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Net.Proto.State;
using UnityEngine;

namespace Net.Proto {
    public sealed class TcpClientStateMachine {
        private readonly TcpClientFSMCtx _context = new();

        private TcpClientStateBase _state;

        public async UniTask StartAsync(TcpClientStateBase initState, CancellationToken token = default) {
            _state = initState;
            while (_state != null) {
                try {
                    _state.OnEnter(_context);
                    await _state.RunAsync(_context, token);
                    _state.OnExit(_context);
                    _state = _state.NextState;
                } catch (Exception e) {
                    Debug.LogErrorFormat("TcpClientState Run Error.\n{0}", e);
                }
            }
        }
    }
}
