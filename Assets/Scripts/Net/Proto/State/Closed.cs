using System.Threading;
using Cysharp.Threading.Tasks;

namespace Net.Proto.State {
    public class Closed : TcpClientStateBase {
        public override async UniTask RunAsync(TcpClientFSMCtx ctx, CancellationToken ct = default) {
            await UniTask.Yield();
        }
    }
}
