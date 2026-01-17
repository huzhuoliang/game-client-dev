using System.Threading;
using Cysharp.Threading.Tasks;

namespace Net.Proto.State {
    public sealed class Connected : TcpClientStateBase {
        protected override async UniTask RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
            await UniTask.Yield();
        }
    }
}
