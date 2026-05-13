using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Net.Tcp.State {
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class Connecting : TcpClientStateBase {
        public override ETcpState Kind => ETcpState.Connecting;
        private Connecting() { }

        protected override async UniTask<TcpClientStateBase> RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
            // 软中断信号：把 ctx.OnDisconnectRequested 挂到 disconnectCts 上，串进 linkedCts
            using CancellationTokenSource disconnectCts = new();
            // ReSharper disable once AccessToDisposedClosure
            Action disconnectHandler = () => disconnectCts.Cancel();
            ctx.OnDisconnectRequested += disconnectHandler;
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, disconnectCts.Token);

            ConnectErrorKind lastErrorKind = ConnectErrorKind.Unknown;
            ctx.OnConnectFailed += OnFailed;
            try {
                for (int i = 0; i < ctx.MaxAttempts; i++) {
                    bool ok = await ctx.Connect(linkedCts.Token);
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
                        await UniTask.Delay(millisecondsDelay: ctx.RetryDelayMs, cancellationToken: linkedCts.Token);
                    }
                }
            } catch (OperationCanceledException) when (disconnectCts.IsCancellationRequested && !ct.IsCancellationRequested) {
                // 软中断（RequestDisconnect）→ 落到下面 return Disconnected
                Debug.LogFormat("Connect to {0} canceled (soft)", ctx.RemoteAddress);
            } catch (OperationCanceledException) {
                // 主 ct 取消 / per-attempt 超时 / 其他 OCE → FSM 整体退出（保持原行为）
                Debug.LogFormat("Connect to {0} canceled", ctx.RemoteAddress);
                return null;
            } finally {
                ctx.OnConnectFailed -= OnFailed;
                ctx.OnDisconnectRequested -= disconnectHandler;
            }
            // 重试耗尽 / TLS 失败 / 软中断 → 走到 Disconnected
            return GetInstance<Disconnected>();

            void OnFailed(ConnectErrorKind kind, Exception _) => lastErrorKind = kind;
        }
    }
}
