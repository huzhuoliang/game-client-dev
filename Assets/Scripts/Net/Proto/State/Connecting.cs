using System;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Net.Proto.State {
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class Connecting : TcpClientStateBase {
        public override void OnEnter(ITcpClientFSMCtx ctx) { }

        protected override async UniTask RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
            try {
                for (int i = 0; i < ctx.MaxAttempts; i++) {
                    TcpClient client = await ctx.Connect(ct);
                    if (client != null) {
                        // ReSharper disable once PossiblyMistakenUseOfCancellationToken
                        // StartLoop(ct); // TODO
                        Debug.LogFormat("Connect to {0} success", ctx.TargetEndPoint);
                        break;
                    }
                    Debug.LogFormat("{0}/{1} Connect to {2} failed", i + 1, ctx.MaxAttempts, ctx.TargetEndPoint);
                    if (ctx.RetryDelayMs > 0) {
                        // ReSharper disable once PossiblyMistakenUseOfCancellationToken
                        await UniTask.Delay(millisecondsDelay: ctx.RetryDelayMs, cancellationToken: ct);
                    }
                }
            } catch (OperationCanceledException) {
                Debug.LogFormat("Connect to {0} canceled", ctx.TargetEndPoint);
            }
        }
    }
}
