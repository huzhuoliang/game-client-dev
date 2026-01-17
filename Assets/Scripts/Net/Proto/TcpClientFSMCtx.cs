using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
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
            CancellationTokenSource cts = new(TimeoutMs);
            CancellationTokenSource tsConn = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);
            TcpClient client = await ConnectOnce(tsConn.Token);
            return client;
        }

        private async UniTask<TcpClient> ConnectOnce(CancellationToken ct) {
            DisposeTcpClient(ref _client);
            _client = await CreateTcpClientAndConnect(TargetEndPoint, ct);
            if (_client == null) {
                return null;
            }
            SslStream sslStream = new SslStream(_client.GetStream(), false, ValidateSeverCertificate);
            SslClientAuthenticationOptions options = new() { TargetHost = "localhost" };
            await sslStream.AuthenticateAsClientAsync(options, ct).AsUniTask();
            _stream = sslStream;
            return Client;
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
                    return null;
                }

                return tcpClient;
            } catch (Exception) when (ct.IsCancellationRequested) {
                /* Task canceled */
                DisposeTcpClient(ref tcpClient);
                return null;
            } catch (SocketException e) {
                Debug.LogFormat("Connect failed.\n{0}", e);
                DisposeTcpClient(ref tcpClient);
                return null;
            } catch (Exception e) {
                Debug.LogErrorFormat("Connect failed.\n{0}", e);
                DisposeTcpClient(ref tcpClient);
                return null;
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
