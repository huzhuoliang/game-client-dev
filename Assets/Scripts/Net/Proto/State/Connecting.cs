using System;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Net.Proto.State {
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class Connecting : TcpClientStateBase {
        private Connecting() { }

        public override void OnEnter(ITcpClientFSMCtx ctx) { }

        protected override async UniTask<TcpClientStateBase> RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
            ConnectErrorKind lastErrorKind = ConnectErrorKind.Unknown;
            ctx.OnConnectFailed += OnFailed;
            try {
                for (int i = 0; i < ctx.MaxAttempts; i++) {
                    TcpClient client = await ctx.Connect(ct);
                    if (client != null) {
                        // ReSharper disable once PossiblyMistakenUseOfCancellationToken
                        // StartLoop(ct); // TODO
                        Debug.LogFormat("Connect to {0} success", ctx.TargetEndPoint);
                        break;
                    }

                    if (lastErrorKind == ConnectErrorKind.TlsAuthFailed) {
                        // 证书/握手类错误重试无意义，提前退出，等 UI/上层处理
                        Debug.LogErrorFormat("Connect to {0} aborted: TLS auth failed.", ctx.TargetEndPoint);
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
            } finally {
                ctx.OnConnectFailed -= OnFailed;
            }
            // TODO 成功 → Handshaking、TLS 失败 → Closed 等转移在 #3 里写
            return null;
            void OnFailed(ConnectErrorKind kind, Exception _) => lastErrorKind = kind;
        }
    }
}
