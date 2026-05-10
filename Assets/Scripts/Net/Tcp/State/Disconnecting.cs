using System.Threading;
using Cysharp.Threading.Tasks;

namespace Net.Tcp.State {
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class Disconnecting : TcpClientStateBase {
        private Disconnecting() { }

        protected override async UniTask<TcpClientStateBase> RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
            await UniTask.Yield();
            return null;
        }
    }
}
