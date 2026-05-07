using System.Threading;
using Cysharp.Threading.Tasks;

namespace Net.Proto.State {
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class Reconnecting : TcpClientStateBase {
        private Reconnecting() { }

        protected override async UniTask<TcpClientStateBase> RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
            await UniTask.Yield();
            return null;
        }
    }
}
