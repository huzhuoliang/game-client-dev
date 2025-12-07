using System.Threading;
using Cysharp.Threading.Tasks;

namespace Net.Proto.State {
    public class Disconnected : TcpClientStateBase {
        public override async UniTask RunAsync(TcpClientFSMCtx ctx, CancellationToken ct = default) {
            await UniTask.Yield();
        }
    }
}
