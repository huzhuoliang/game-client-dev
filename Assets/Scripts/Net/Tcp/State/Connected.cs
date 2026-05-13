using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Net.Tcp.State {
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class Connected : TcpClientStateBase {
        public override ETcpState Kind => ETcpState.Connected;
        private Connected() { }

        protected override async UniTask<TcpClientStateBase> RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
            // disconnectCts：把 ctx.OnDisconnectRequested 软中断信号挂到一个 ct 上，串进 RunMessagePump
            using CancellationTokenSource disconnectCts = new();
            Action handler = () => disconnectCts.Cancel();
            ctx.OnDisconnectRequested += handler;
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, disconnectCts.Token);

            try {
                await ctx.RunMessagePump(linkedCts.Token);
            } catch (OperationCanceledException) when (ct.IsCancellationRequested) {
                throw;                                          // 主 ct 取消 → FSM 紧急退出（不走 Disconnecting）
            } catch (OperationCanceledException) {
                // 软中断（RequestDisconnect 触发）→ 走到下面 return Disconnecting
            } catch (Exception e) {
                Debug.LogFormat("Connection broken: {0}", e.Message);
            } finally {
                ctx.OnDisconnectRequested -= handler;
            }
            return GetInstance<Disconnecting>();
        }
    }
}
