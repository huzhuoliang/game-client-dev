using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Net.Tcp.State {
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class Connected : TcpClientStateBase {
        private Connected() { }

        protected override async UniTask<TcpClientStateBase> RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
            try {
                await ctx.RunMessagePump(ct);
            } catch (OperationCanceledException) {
                throw;                                          // 顶层 ct 取消 → FSM 整体退出
            } catch (Exception e) {
                Debug.LogFormat("Connection broken: {0}", e.Message);
            }
            return GetInstance<Disconnecting>();
        }
    }
}
