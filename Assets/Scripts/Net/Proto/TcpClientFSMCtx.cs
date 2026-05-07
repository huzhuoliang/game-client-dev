using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Net.Proto {
    /// <summary>
    /// Tcp Client State Machine Context
    /// </summary>
    public sealed class TcpClientFSMCtx : ITcpClientFSMCtx {
        public IPEndPoint TargetEndPoint { get; private set; }

        public int MaxAttempts { get; private set; }

        public int TimeoutMs { get; private set; }

        public int RetryDelayMs { get; private set; }

        public event Action<ConnectErrorKind, Exception> OnConnectFailed;

        private TcpClient _client;
        public TcpClient Client => _client;

        private SslStream _stream;

        public TcpClientFSMCtx(
                IPEndPoint targetEndPoint,
                int maxAttempts = 10,
                int timeoutMs = 5000,
                int retryDelayMs = 1000
        ) {
            TargetEndPoint = targetEndPoint;
            MaxAttempts = maxAttempts;
            TimeoutMs = timeoutMs;
            RetryDelayMs = retryDelayMs;
        }

        public TcpClientFSMCtx(
                string host,
                int port,
                int maxAttempts = 10,
                int timeoutMs = 5000,
                int retryDelayMs = 1000
        ) : this(CreateEndPointFromHost(host, port), maxAttempts, timeoutMs, retryDelayMs) { }

        private static IPEndPoint CreateEndPointFromHost(string host, int port) {
            IPAddress ip;
            if (IPAddress.TryParse(host, out IPAddress addr)) {
                ip = addr;
            } else {
                ip = Dns.GetHostAddressesAsync(host).AsUniTask().GetAwaiter().GetResult()[0];
            }
            IPEndPoint targetEndPoint = new IPEndPoint(ip, port);
            return targetEndPoint;
        }

        public async UniTask<TcpClient> Connect(CancellationToken ct) {
            using CancellationTokenSource timeoutCts = new(TimeoutMs);
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
            return await ConnectOnce(linkedCts.Token);
        }

        private async UniTask<TcpClient> ConnectOnce(CancellationToken ct) {
            DisposeTcpClient(ref _client);

            // Phase 1: TCP 连接
            try {
                _client = await CreateTcpClientAndConnect(TargetEndPoint, ct);
            } catch (OperationCanceledException) {
                // 取消透传
                throw;
            } catch (SocketException e) {
                Debug.LogFormat("Connect failed (socket): {0}", e.Message);
                OnConnectFailed?.Invoke(ConnectErrorKind.SocketError, e);
                return null;
            } catch (Exception e) {
                Debug.LogErrorFormat("Connect failed (unknown): {0}", e);
                OnConnectFailed?.Invoke(ConnectErrorKind.Unknown, e);
                return null;
            }

            if (_client == null) {
                return null;
            }

            // Phase 2: TLS 握手
            try {
                SslStream sslStream = new SslStream(_client.GetStream(), false, ValidateSeverCertificate);
                SslClientAuthenticationOptions options = new() { TargetHost = "localhost" };
                await sslStream.AuthenticateAsClientAsync(options, ct).AsUniTask();
                _stream = sslStream;
                return _client;
            } catch (OperationCanceledException) {
                DisposeTcpClient(ref _client);
                throw;
            } catch (AuthenticationException e) {
                // 证书不被信任 / CN 不匹配 / 协议不兼容等。重试通常无效。
                Debug.LogErrorFormat("TLS handshake failed: {0}", e.Message);
                OnConnectFailed?.Invoke(ConnectErrorKind.TlsAuthFailed, e);
                DisposeTcpClient(ref _client);
                return null;
            } catch (Exception e) {
                Debug.LogErrorFormat("SSL init failed: {0}", e);
                OnConnectFailed?.Invoke(ConnectErrorKind.Unknown, e);
                DisposeTcpClient(ref _client);
                return null;
            }
        }

        private static bool ValidateSeverCertificate(
                object sender,
                X509Certificate certificate,
                X509Chain chain,
                SslPolicyErrors sslPolicyErrors) {
            return sslPolicyErrors == SslPolicyErrors.None;
        }

        private static async UniTask<TcpClient> CreateTcpClientAndConnect(IPEndPoint ipEndPoint, CancellationToken ct) {
            TcpClient tcpClient = new TcpClient();
            try {
                Debug.LogFormat("Try connect {0} ...", ipEndPoint);
                UniTask connectTask = tcpClient.ConnectAsync(ipEndPoint.Address, ipEndPoint.Port).AsUniTask();
                UniTask cancelTask = UniTask.WaitUntilCanceled(ct);
                int index = await UniTask.WhenAny(connectTask, cancelTask);
                if (index == 1) {
                    DisposeTcpClient(ref tcpClient);
                    ct.ThrowIfCancellationRequested();
                }
                return tcpClient;
            } catch {
                // 异常情况下确保清理，让上层按类型分发
                DisposeTcpClient(ref tcpClient);
                throw;
            }
        }

        private static void DisposeTcpClient(ref TcpClient client) {
            client?.Close();
            client?.Dispose();
            client = null;
        }

        public void Dispose() {
            DisposeTcpClient(ref _client);
        }
    }
}
