using System;
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
    public sealed class TcpClientFSMCtx : IDisposable {
        public string Addr;
        public int Port;

        public int MaxAttempts = 10;
        public int TimeoutMs = 5000;
        public int RetryDelayMs = 1000;

        private TcpClient _client;
        public TcpClient Client => _client;

        private SslStream _stream;

        public async UniTask<TcpClient> Connect(CancellationToken ct) {
            CancellationTokenSource cts = new(TimeoutMs);
            CancellationTokenSource tsConn = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);
            TcpClient client = await ConnectOnce(tsConn.Token);
            return client;
        }

        private async UniTask<TcpClient> ConnectOnce(CancellationToken ct) {
            DisposeTcpClient(ref _client);
            _client = await CreateTcpClientAndConnect(Addr, Port, ct);
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

        private static async UniTask<TcpClient> CreateTcpClientAndConnect(string addr, int port, CancellationToken ct) {
            TcpClient tcpClient = new TcpClient();
            try {
                Debug.LogFormat("Try connect {0}:{1} ...", addr, port);
                UniTask connectTask = tcpClient.ConnectAsync(addr, port).AsUniTask();
                UniTask cancelTask = UniTask.WaitUntilCanceled(ct);
                int index = await UniTask.WhenAny(connectTask, cancelTask);
                if (index == 1) {
                    DisposeTcpClient(ref tcpClient);
                    return null;
                }

                return tcpClient;
            } catch (Exception) when (ct.IsCancellationRequested) {
                /* Task cancelled */
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
