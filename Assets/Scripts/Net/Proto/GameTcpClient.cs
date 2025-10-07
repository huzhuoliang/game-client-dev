using System;
using System.Buffers;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Game.Util;
using GameServerServices.MessageType;
using Google.Protobuf;
using UnityEngine;
using UnityEngine.UI;

namespace Net.Proto {
    public class GameTcpClient : IDisposable {
        private const int MAGIC_NUMBER_SIZE = 4;
        private const int TYPE_SIZE = 2;
        private const int LENGTH_SIZE = 4;
        // private const int CRC_SIZE = 4;

        private const uint MAGIC_NUMBER = 0xCAFEBABE;

        private byte[] _magicNumberBytes;

        private byte[] MagicNumberBytes => _magicNumberBytes ??= MAGIC_NUMBER.GetBytesBigEndian();

        private string _addr = "192.168.50.16";
        private int _port = 50052;

        /// <summary>
        /// [Magic] [Type] [Length] [CRC32] [Body]
        /// </summary>
        // private const int HEADER_SIZE = MAGIC_NUMBER_SIZE + TYPE_SIZE + LENGTH_SIZE;
        private const int MAX_BODY_SIZE = 1024 * 1024; // 1 MB

        private TcpClient _client;

        private SslStream _stream;

        private readonly RingBufferStream _ringBufferStream = new();

        private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

        public bool Connected { get; private set; }

        public event Action OnClose;

        private CancellationTokenSource _cts;

        private bool _isInit;

        private UniTask? _listenTask;
        private UniTask? _parseTask;
        private bool _isClosing;

        private void Init() {
            if (_isInit) {
                return;
            }

            RegisterAll();
            Connected = false;
            _isInit = true;
            _isClosing = false;
        }

        private static void RegisterAll() {
#if UNITY_EDITOR
            RegisterAllByReflection();
#else
            /* TODO 调用自动生成的类来注册 */
            // MessageHandlerRegistry.Instance.Register(new TcpHelloWorldHandler());
#endif
        }

        private static void UnRegisterAll() {
            MessageHandlerRegistry.Instance.UnRegisterAll();
        }

        private static void RegisterAllByReflection() {
            Type handlerInterface = typeof(IMessageHandler<>);

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes()) {
                if (!type.IsClass || type.IsAbstract) {
                    continue;
                }

                Type[] interfaces = type.GetInterfaces();
                Type interfaceType = null;
                foreach (Type inter in interfaces) {
                    if (inter.IsGenericType && inter.GetGenericTypeDefinition() == handlerInterface) {
                        interfaceType = inter;
                    }
                }

                if (interfaceType != null) {
                    object handlerInstance = Activator.CreateInstance(type);
                    MethodInfo registerMethod = typeof(MessageHandlerRegistry).GetMethod("Register");
                    registerMethod = registerMethod?.MakeGenericMethod(interfaceType.GenericTypeArguments[0]);
                    registerMethod?.Invoke(MessageHandlerRegistry.Instance, new[] { handlerInstance });
                    // Debug.Log($"[AutoRegister] Registered {type.Name}");
                }
            }
        }

        public async UniTask StartConnectionAsync(string addr, int port, int timeoutMs = 5000, int retryDelayMs = 1000, int maxAttempts = 10) {
            if (_isClosing) {
                return;
            }

            CloseInternalAsync().Forget();
            _cts = new CancellationTokenSource();
            await StartConnectionInternalAsync(addr, port, timeoutMs, retryDelayMs, maxAttempts, _cts.Token);
        }

        private async UniTask StartConnectionInternalAsync(string addr, int port, int timeoutMs = 5000, int retryDelayMs = 1000, int maxAttempts = 10, CancellationToken token = default) {
            Init();
            bool success = false;
            try {
                for (int i = 0; i < maxAttempts; i++) {
                    CancellationTokenSource ts = new(timeoutMs);
                    CancellationTokenSource tsConn = CancellationTokenSource.CreateLinkedTokenSource(token, ts.Token);
                    bool result = await TryConnectOnce(tsConn.Token, addr, port);
                    success = result;
                    Connected = success;
                    if (success) {
                        // ReSharper disable once PossiblyMistakenUseOfCancellationToken
                        StartLoop(token);
                        _addr = addr;
                        _port = port;
                        Debug.LogFormat("Connect to {0}:{1} success", addr, port);
                        break;
                    }

                    Debug.LogFormat("Connect to {0}:{1} attempt {2}/{3} failed", addr, port, i + 1, maxAttempts);

                    if (retryDelayMs > 0) {
                        // ReSharper disable once PossiblyMistakenUseOfCancellationToken
                        await UniTask.Delay(millisecondsDelay: retryDelayMs, cancellationToken: token);
                    }
                }
            } catch (OperationCanceledException) {
                Debug.LogFormat("Connect to {0}:{1} canceled", addr, port);
            }

            if (!success) {
                Debug.LogFormat("Connect to {0}:{1} attempt abort", addr, port);
                CloseInternalAsync().Forget();
                OnClose?.Invoke();
            }
        }

        private async UniTask<bool> TryConnectOnce(CancellationToken token, string addr, int port) {
            if (_client != null) {
                _client.Close();
            }

            _client = await TcpConnect(token, addr, port);
            if (_client == null) {
                return false;
            }

            SslStream sslStream = new SslStream(_client.GetStream(), false, ValidateSeverCertificate);
            SslClientAuthenticationOptions options = new() { TargetHost = "localhost" };
            await sslStream.AuthenticateAsClientAsync(options, token);
            _stream = sslStream;

            return true;
        }

        private void StartLoop(CancellationToken token) {
            _listenTask = ListenLoopAsync(token);
            _parseTask = ParseLoopAsync(token);
        }

        private static async UniTask<TcpClient> TcpConnect(CancellationToken token, string addr, int port) {
            TcpClient tcpClient = new TcpClient();
            try {
                Debug.LogFormat("Try connect {0}:{1} ...", addr, port);
                UniTask connectTask = tcpClient.ConnectAsync(addr, port).AsUniTask();
                UniTask cancelTask = UniTask.WaitUntilCanceled(token);
                int index = await UniTask.WhenAny(connectTask, cancelTask);
                if (index == 1) {
                    tcpClient.Close();
                    return null;
                }

                return tcpClient;
            } catch (Exception) when (token.IsCancellationRequested) {
                /* Task cancelled */
                tcpClient.Close();
                return null;
            } catch (SocketException e) {
                Debug.LogFormat("Connect failed.\n{0}", e);
                tcpClient.Close();
                return null;
            } catch (Exception e) {
                Debug.LogErrorFormat("Connect failed.\n{0}", e);
                tcpClient.Close();
                return null;
            }
        }

        private static bool ValidateSeverCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
            return sslPolicyErrors == SslPolicyErrors.None;
        }

        public void Close() {
            CloseInternalAsync().Forget();
            OnClose?.Invoke();
        }

        private async UniTask CloseInternalAsync() {
            if (_isClosing) {
                return;
            }
            _isClosing = true;
            _cts?.Cancel();
            _stream?.Close();
            _stream?.DisposeAsync();
            _stream = null;
            if (_listenTask.HasValue) {
                await _listenTask.Value;
            }
            _listenTask = null;
            if (_parseTask.HasValue) {
                await _parseTask.Value;
            }
            _parseTask = null;
            _client?.Close();
            _client?.Dispose();
            _client = null;
            _cts?.Dispose();
            _cts = null;
            Connected = false;
            _isClosing = false;
        }

        public void SendMessage(MessageType messageType, IMessage message) {
            ushort msgType = (ushort)messageType;
            byte[] bodyBytes = message.ToByteArray();
            byte[] lengthBytes = ((uint)bodyBytes.Length).GetBytesBigEndian();
            byte[] msgTypeBytes = msgType.GetBytesBigEndian();

            using MemoryStream mem = new MemoryStream();

            mem.Write(MagicNumberBytes, 0, 4);
            mem.Write(msgTypeBytes, 0, 2);
            mem.Write(lengthBytes, 0, 4);
            mem.Write(bodyBytes, 0, bodyBytes.Length);
            uint crc = Crc32.Compute(bodyBytes, 0, bodyBytes.Length);
            byte[] crcBytes = crc.GetBytesBigEndian();
            mem.Write(crcBytes, 0, 4);

            byte[] payload = mem.ToArray();
            _stream.Write(payload, 0, payload.Length);
        }

        private async UniTask ListenLoopAsync(CancellationToken ct) {
            try {
                byte[] temp = new byte[1024];
                while (!ct.IsCancellationRequested) {
                    int read = await _stream.ReadAsync(temp, 0, temp.Length, ct).AsUniTask();
                    if (read == 0) {
                        throw new IOException("Disconnected");
                    }

                    _ringBufferStream.Push(temp, 0, read);
                }
            } catch (ObjectDisposedException) {
                // _stream 被 Disposed 掉了
            } catch (OperationCanceledException) {
                // 连接结束: 主动断开
            } catch (IOException e) {
                if (e.Message.Equals("Disconnected")) {
                    // 连接结束: 服务器断开连接
                    CloseInternalAsync().Forget();
                    OnClose?.Invoke();
                } else {
                    // 连接结束
                    Debug.LogErrorFormat("连接结束: {0}", e);
                }
            } catch (Exception e) {
                // 连接异常
                Debug.LogErrorFormat("连接异常: {0}", e);
            }
        }

        private async UniTask ParseLoopAsync(CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                try {
                    bool haveMagicNumber = await ParseMagicNumber(ct);
                    if (haveMagicNumber) {
                        await ParseOther(ct);
                    }
                } catch (OperationCanceledException) {
                    // 主动取消
                }
            }
        }

        private async UniTask<bool> ParseMagicNumber(CancellationToken ct) {
            int matched = 0;
            while (matched < MAGIC_NUMBER_SIZE && !ct.IsCancellationRequested) {
                byte b = await _ringBufferStream.ReadByteAsync(ct);
                if (b == MagicNumberBytes[matched]) {
                    matched++;
                } else {
                    if (b == MagicNumberBytes[0]) {
                        matched = 1;
                    } else {
                        matched = 0;
                    }
                }
            }

            return true;
        }

        private async UniTask ParseOther(CancellationToken ct) {
            ushort msgType = await _ringBufferStream.ReadUint16(ct);
            MessageType messageType = (MessageType)msgType;
            int length = (int)await _ringBufferStream.ReadUint32(ct);
            if (length > MAX_BODY_SIZE) {
                Debug.LogErrorFormat("Length too large: {0}", length);
                return;
            }

            byte[] dataBuffer = _bufferPool.Rent(length);

            try {
                await _ringBufferStream.ReadBytesAsync(dataBuffer, 0, length, ct);

                uint crc = await _ringBufferStream.ReadUint32(ct);
                uint expiredCrc = Crc32.Compute(dataBuffer, 0, length);
                if (expiredCrc != crc) {
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
                _bufferPool.Return(dataBuffer);
            }
        }

        public void Dispose() {
            UnRegisterAll();
            _client?.Dispose();
            _client = null;
            _stream?.Dispose();
            _stream = null;
            _cts?.Dispose();
            _cts = null;
            _ringBufferStream?.Dispose();
        }
    }
}
