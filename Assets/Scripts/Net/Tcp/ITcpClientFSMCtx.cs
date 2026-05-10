using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameServerServices.MessageType;
using Google.Protobuf;

namespace Net.Tcp {
    public interface ITcpClientFSMCtx : IDisposable {
        /// <summary>
        /// 目标地址的显示字符串（"host:port"）。无论 DNS 是否已解析都可用，仅用于日志/UI。
        /// </summary>
        string RemoteAddress { get; }

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
        /// 建立 TCP 连接 + 完成 TLS 握手。成功返回 true；失败返回 false（同时已触发 <see cref="OnConnectFailed"/>）；
        /// 取消抛 OperationCanceledException。
        /// </summary>
        UniTask<bool> Connect(CancellationToken ct = default);

        /// <summary>
        /// 发送一条消息：自动套上 wire format header（Magic + Type + Length + Body + CRC32，全大端）。
        /// 写入串行化（内部 SemaphoreSlim），并发调用安全。
        /// 调用时必须已连接，否则抛 InvalidOperationException。
        /// </summary>
        UniTask SendMessage(MessageType messageType, IMessage message, CancellationToken ct = default);

        /// <summary>
        /// 启动并阻塞 listen + parse 双循环（消息泵）：socket → ring buffer → 解帧 → handler 分发。
        /// 任一循环退出 / `ct` 取消时一起 drain 后返回。
        /// 由 `Connected` 状态调用——OCE 会被透传，IOException / 协议错原样抛。
        /// </summary>
        UniTask RunMessagePump(CancellationToken ct);
    }
}
