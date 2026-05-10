using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Util;
using GameServerServices.MessageType;
using Google.Protobuf;
using UnityEngine;

namespace Net.Tcp {
    /// <summary>
    /// Tcp Client State Machine Context
    /// </summary>
    public sealed class TcpClientFSMCtx : ITcpClientFSMCtx {
        private const uint MAGIC_NUMBER = 0xCAFEBABE;
        private const int MAGIC_NUMBER_SIZE = 4;
        private const int MAX_BODY_SIZE = 1024 * 1024;

        private static readonly byte[] sMagicNumberBytes = MAGIC_NUMBER.GetBytesBigEndian();
        private static readonly ArrayPool<byte> sBufferPool = ArrayPool<byte>.Shared;

        public int MaxAttempts { get; private set; }

        public int TimeoutMs { get; private set; }

        public int RetryDelayMs { get; private set; }

        public event Action<ConnectErrorKind, Exception> OnConnectFailed;

        private readonly string _host;
        private readonly int _port;

        // TLS SNI / 证书 CN 校验用的 host name；与 _host 解耦——可指向 IP 但要求证书 SAN 是某域名
        private readonly string _targetHost;

        // DNS 解析后缓存；构造时若给的是 IPEndPoint 则直接填充，否则首次 Connect 时填充
        private IPEndPoint _targetEndPoint;
        public string RemoteAddress => _targetEndPoint?.ToString() ?? $"{_host}:{_port}";

        private TcpClient _client;
        private SslStream _stream;
        private readonly RingBufferStream _ringBuffer = new();

        // SslStream 不允许并发写：用信号量串行化所有 SendMessage 调用
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public TcpClientFSMCtx(
                IPEndPoint targetEndPoint,
                int maxAttempts = 10,
                int timeoutMs = 5000,
                int retryDelayMs = 1000,
                string targetHost = "localhost"
        ) {
            _targetEndPoint = targetEndPoint;
            _host = targetEndPoint.Address.ToString();
            _port = targetEndPoint.Port;
            _targetHost = targetHost;
            MaxAttempts = maxAttempts;
            TimeoutMs = timeoutMs;
            RetryDelayMs = retryDelayMs;
        }

        public TcpClientFSMCtx(
                string host,
                int port,
                int maxAttempts = 10,
                int timeoutMs = 5000,
                int retryDelayMs = 1000,
                string targetHost = "localhost"
        ) {
            _host = host;
            _port = port;
            _targetHost = targetHost;
            // _targetEndPoint 留 null，Connect() 第一步异步解析 DNS 时填充
            MaxAttempts = maxAttempts;
            TimeoutMs = timeoutMs;
            RetryDelayMs = retryDelayMs;
        }

        public async UniTask<bool> Connect(CancellationToken ct) {
            using CancellationTokenSource timeoutCts = new(TimeoutMs);
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            if (_targetEndPoint == null) {
                bool resolved = await ResolveEndPoint(linkedCts.Token);
                if (!resolved) {
                    return false;
                }
            }

            return await ConnectOnce(linkedCts.Token);
        }

        private async UniTask<bool> ResolveEndPoint(CancellationToken ct) {
            // 字面量 IP 直接构造，跳过 DNS
            if (IPAddress.TryParse(_host, out IPAddress literal)) {
                _targetEndPoint = new IPEndPoint(literal, _port);
                return true;
            }

            try {
                IPAddress[] addresses = await Dns.GetHostAddressesAsync(_host)
                        .AsUniTask()
                        .AttachExternalCancellation(ct);
                if (addresses == null || addresses.Length == 0) {
                    throw new SocketException((int)SocketError.HostNotFound);
                }
                _targetEndPoint = new IPEndPoint(addresses[0], _port);
                return true;
            } catch (OperationCanceledException) {
                throw;
            } catch (SocketException e) {
                Debug.LogFormat("DNS resolve failed for {0}: {1}", _host, e.Message);
                OnConnectFailed?.Invoke(ConnectErrorKind.SocketError, e);
                return false;
            } catch (Exception e) {
                Debug.LogErrorFormat("DNS resolve failed (unknown): {0}", e);
                OnConnectFailed?.Invoke(ConnectErrorKind.Unknown, e);
                return false;
            }
        }

        private async UniTask<bool> ConnectOnce(CancellationToken ct) {
            DisposeTcpClient(ref _client);

            // Phase 1: TCP 连接
            try {
                _client = await CreateTcpClientAndConnect(_targetEndPoint, ct);
            } catch (OperationCanceledException) {
                // 取消透传
                throw;
            } catch (SocketException e) {
                Debug.LogFormat("Connect failed (socket): {0}", e.Message);
                OnConnectFailed?.Invoke(ConnectErrorKind.SocketError, e);
                return false;
            } catch (Exception e) {
                Debug.LogErrorFormat("Connect failed (unknown): {0}", e);
                OnConnectFailed?.Invoke(ConnectErrorKind.Unknown, e);
                return false;
            }

            if (_client == null) {
                return false;
            }

            // Phase 2: TLS 握手
            try {
                SslStream sslStream = new SslStream(_client.GetStream(), false, ValidateSeverCertificate);
                SslClientAuthenticationOptions options = new() { TargetHost = _targetHost };
                await sslStream.AuthenticateAsClientAsync(options, ct).AsUniTask();
                _stream = sslStream;
                return true;
            } catch (OperationCanceledException) {
                DisposeTcpClient(ref _client);
                throw;
            } catch (AuthenticationException e) {
                // 证书不被信任 / CN 不匹配 / 协议不兼容等。重试通常无效。
                Debug.LogErrorFormat("TLS handshake failed: {0}", e.Message);
                OnConnectFailed?.Invoke(ConnectErrorKind.TlsAuthFailed, e);
                DisposeTcpClient(ref _client);
                return false;
            } catch (Exception e) {
                Debug.LogErrorFormat("SSL init failed: {0}", e);
                OnConnectFailed?.Invoke(ConnectErrorKind.Unknown, e);
                DisposeTcpClient(ref _client);
                return false;
            }
        }

        public async UniTask SendMessage(MessageType messageType, IMessage message, CancellationToken ct = default) {
            Stream stream = _stream;
            if (stream == null) {
                throw new InvalidOperationException("SendMessage called while not connected");
            }

            byte[] payload = BuildFrame(messageType, message);

            await _writeLock.WaitAsync(ct).AsUniTask();
            try {
                await stream.WriteAsync(payload, 0, payload.Length, ct).AsUniTask();
            } finally {
                _writeLock.Release();
            }
        }

        public async UniTask RunMessagePump(CancellationToken ct) {
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            UniTask listen = ListenLoopAsync(linkedCts.Token).Preserve();
            UniTask parse  = ParseLoopAsync(linkedCts.Token).Preserve();

            try {
                int idx = await UniTask.WhenAny(listen, parse);
                // 让先完成的循环把异常抛出（如果有）
                if (idx == 0) {
                    await listen;
                } else {
                    await parse;
                }
            } finally {
                // 不管哪条循环先停，把另一条一起叫停后 drain，避免泄漏
                linkedCts.Cancel();
                try { await listen; } catch { /* expected on cleanup */ }
                try { await parse; }  catch { /* expected on cleanup */ }
            }
        }

        private async UniTask ListenLoopAsync(CancellationToken ct) {
            Stream stream = _stream;
            if (stream == null) {
                throw new InvalidOperationException("ListenLoop: stream not ready");
            }
            byte[] temp = new byte[1024];
            while (!ct.IsCancellationRequested) {
                int read = await stream.ReadAsync(temp, 0, temp.Length, ct).AsUniTask();
                if (read == 0) {
                    throw new IOException("Disconnected");
                }
                _ringBuffer.Push(temp, 0, read);
            }
        }

        private async UniTask ParseLoopAsync(CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                await ParseMagicNumber(ct);
                await ParseOther(ct);
            }
        }

        private async UniTask ParseMagicNumber(CancellationToken ct) {
            int matched = 0;
            while (matched < MAGIC_NUMBER_SIZE && !ct.IsCancellationRequested) {
                byte b = await _ringBuffer.ReadByteAsync(ct);
                if (b == sMagicNumberBytes[matched]) {
                    matched++;
                } else {
                    matched = b == sMagicNumberBytes[0] ? 1 : 0;
                }
            }
        }

        private async UniTask ParseOther(CancellationToken ct) {
            ushort msgType = await _ringBuffer.ReadUint16(ct);
            MessageType messageType = (MessageType)msgType;
            int length = (int)await _ringBuffer.ReadUint32(ct);
            if (length > MAX_BODY_SIZE) {
                Debug.LogErrorFormat("Length too large: {0}", length);
                return;
            }

            byte[] dataBuffer = sBufferPool.Rent(length);
            try {
                await _ringBuffer.ReadBytesAsync(dataBuffer, 0, length, ct);
                uint crc = await _ringBuffer.ReadUint32(ct);
                uint expectedCrc = Crc32.Compute(dataBuffer, 0, length);
                if (expectedCrc != crc) {
                    Debug.LogErrorFormat("[Client] CRC check failed: messageType={0}", messageType);
                    return;
                }
                if (!MessageHandlerRegistry.Instance.TryGetHandler(messageType, out IMessageHandlerWrapper wrapper)) {
                    Debug.LogErrorFormat("[Client] No message handler: messageType={0}", messageType);
                    return;
                }
                IMessage msg = wrapper.CreateMessage();
                msg.MergeFrom(new CodedInputStream(dataBuffer, 0, length));
                wrapper.Handle(msg);
            } catch (Exception e) {
                Debug.LogException(e);
                throw;
            } finally {
                sBufferPool.Return(dataBuffer);
            }
        }

        private static byte[] BuildFrame(MessageType messageType, IMessage message) {
            byte[] body = message.ToByteArray();
            uint crc = Crc32.Compute(body, 0, body.Length);

            using MemoryStream mem = new MemoryStream(4 + 2 + 4 + body.Length + 4);
            mem.Write(sMagicNumberBytes, 0, 4);
            mem.Write(((ushort)messageType).GetBytesBigEndian(), 0, 2);
            mem.Write(((uint)body.Length).GetBytesBigEndian(), 0, 4);
            mem.Write(body, 0, body.Length);
            mem.Write(crc.GetBytesBigEndian(), 0, 4);
            return mem.ToArray();
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
            _ringBuffer?.Dispose();
            _writeLock?.Dispose();
        }
    }
}
