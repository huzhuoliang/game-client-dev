using System.Threading;
using Cysharp.Threading.Tasks;

namespace Net.Tcp.State {
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class Closed : TcpClientStateBase {
        private Closed() { }

        protected override async UniTask<TcpClientStateBase> RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
            await UniTask.Yield();
            return null;
        }
    }
}
