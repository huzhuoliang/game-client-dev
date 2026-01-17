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
        /// 建立 TCP 连接（返回已连接的 TcpClient，或 null 表示失败）
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        UniTask<TcpClient> Connect(CancellationToken ct = default);
    }
}
