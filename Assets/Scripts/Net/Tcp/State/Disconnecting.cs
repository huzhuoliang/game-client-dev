using System.Threading;
using Cysharp.Threading.Tasks;

namespace Net.Tcp.State {
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class Disconnecting : TcpClientStateBase {
        private Disconnecting() { }

        protected override async UniTask<TcpClientStateBase> RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
            // 关闭操作 best-effort、不接受 ct——这是清理动作，必须跑完，不能被取消
            await ctx.CloseConnection();
            return GetInstance<Disconnected>();
        }
    }
}
