using System;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Net.Proto.State {
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Connecting : TcpClientStateBase {
        public override void OnEnter(TcpClientFSMCtx ctx) { }

        public override async UniTask RunAsync(TcpClientFSMCtx ctx, CancellationToken ct = default) {
            Debug.LogErrorFormat("============ Connecting.RunAsync");
            try {
                for (int i = 0; i < ctx.MaxAttempts; i++) {
                    TcpClient client = await ctx.Connect(ct);
                    if (client != null) {
                        // ReSharper disable once PossiblyMistakenUseOfCancellationToken
                        // StartLoop(ct); // TODO
                        Debug.LogFormat("Connect to {0}:{1} success", ctx.Addr, ctx.Port);
                        break;
                    }
                    Debug.LogFormat("{0}/{1} Connect to {2}:{3} failed", i + 1, ctx.MaxAttempts, ctx.Addr, ctx.Port);
                    if (ctx.RetryDelayMs > 0) {
                        // ReSharper disable once PossiblyMistakenUseOfCancellationToken
                        await UniTask.Delay(millisecondsDelay: ctx.RetryDelayMs, cancellationToken: ct);
                    }
                }
            } catch (OperationCanceledException) {
                Debug.LogFormat("Connect to {0}:{1} canceled", ctx.Addr, ctx.Port);
            }
        }
    }
}
