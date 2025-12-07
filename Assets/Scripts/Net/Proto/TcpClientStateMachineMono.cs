using System.Threading;
using Cysharp.Threading.Tasks;
using Net.Proto.State;
using UnityEngine;

namespace Net.Proto {
    public class TcpClientStateMachineMono : MonoBehaviour {
        private TcpClientStateMachine _stateMachine;
        private CancellationTokenSource _cts;

        private void Awake() {
            _stateMachine = new TcpClientStateMachine();
        }

        private void OnEnable() {
            _cts = new CancellationTokenSource();
            _stateMachine.StartAsync(new Init(), _cts.Token).Forget();
        }

        private void OnDisable() {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
