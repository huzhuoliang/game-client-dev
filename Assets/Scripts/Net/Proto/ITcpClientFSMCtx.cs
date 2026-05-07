using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Net.Proto {
    public interface ITcpClientFSMCtx : IDisposable {
        /// <summary>
        /// 目标端点（IP + 端口）
        /// </summary>
        IPEndPoint TargetEndPoint { get; }

        /// <summary>
        /// 最大重试次数
        /// </summary>
        int MaxAttempts { get; }

        /// <summary>
        /// 重试延迟（毫秒）
        /// </summary>
        int RetryDelayMs { get; }

        /// <summary>
        /// 单次连接尝试失败时触发（每次失败/重试都会触发一次）。
        /// 取消（OperationCanceledException）不会触发，会以异常形式向上抛。
        /// 供 UI 订阅以做用户提示。
        /// </summary>
        event Action<ConnectErrorKind, Exception> OnConnectFailed;

        /// <summary>
        /// 建立 TCP 连接 + 完成 TLS 握手。失败返回 null（同时已触发 <see cref="OnConnectFailed"/>）；
        /// 取消抛 OperationCanceledException。
        /// </summary>
        UniTask<TcpClient> Connect(CancellationToken ct = default);
    }
}
