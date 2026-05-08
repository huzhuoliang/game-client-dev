using System;
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
                    bool ok = await ctx.Connect(ct);
                    if (ok) {
                        Debug.LogFormat("Connect to {0} success", ctx.RemoteAddress);
                        return GetInstance<Connected>();
                    }

                    if (lastErrorKind == ConnectErrorKind.TlsAuthFailed) {
                        // 证书/握手类错误重试无意义，提前退出，等 UI/上层处理
                        Debug.LogErrorFormat("Connect to {0} aborted: TLS auth failed.", ctx.RemoteAddress);
                        break;
                    }

                    Debug.LogFormat("{0}/{1} Connect to {2} failed", i + 1, ctx.MaxAttempts, ctx.RemoteAddress);
                    if (ctx.RetryDelayMs > 0) {
                        // ReSharper disable once PossiblyMistakenUseOfCancellationToken
                        await UniTask.Delay(millisecondsDelay: ctx.RetryDelayMs, cancellationToken: ct);
                    }
                }
            } catch (OperationCanceledException) {
                // 取消是正常退出路径，由状态机顶层处理；此处只记录信息
                Debug.LogFormat("Connect to {0} canceled", ctx.RemoteAddress);
                return null;
            } finally {
                ctx.OnConnectFailed -= OnFailed;
            }
            // 重试耗尽 / TLS 失败 → 走到 Disconnected
            return GetInstance<Disconnected>();
            void OnFailed(ConnectErrorKind kind, Exception _) => lastErrorKind = kind;
        }
    }
}
