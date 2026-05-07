using System.Threading;
using Cysharp.Threading.Tasks;
using Net.Proto.State;
using UnityEngine;

namespace Net.Proto {
    public class TcpClientStateMachineMono : MonoBehaviour {
        private TcpClientStateMachine _stateMachine;
        private CancellationTokenSource _cts;

        private void Awake() {
            TcpClientFSMCtx ctx = new("192.168.50.16", 50052);
            _stateMachine = new TcpClientStateMachine(ctx);
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
    }
}
