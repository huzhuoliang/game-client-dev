using System;
using System.Buffers;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Game.Util;
using GameServerServices.MessageType;
using Google.Protobuf;
using UnityEngine;

namespace Net.Proto {
    public class GameTcpClient {
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
        private NetworkStream _stream;
        private CancellationTokenSource _cts;

        private readonly RingBufferStream _ringBufferStream = new();

        private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

        public bool Connected {
            get {
                TcpClient client = _client;
                try {
                    if (client?.Client == null) return false;

                    Socket socket = client.Client;
                    return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
                } catch {
                    return false;
                }
            }
        }

        public bool IsStart { get; private set; }

        private bool _isClientClosing;

        private bool _hasClosed; // 确保 OnClose 事件只被调用一次

        public event Action OnClose;

        private void Init() {
            if (IsStart) {
                return;
            }

            RegisterAll();
            IsStart = true;
            _isClientClosing = false;
            _hasClosed = false;
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

        private CancellationTokenSource _attemptConnectCts;
        private CancellationTokenSource _connectTimeoutCts;

        public async Task StartConnectionAsync(string addr, int port, int timeoutMs = 5000, int retryDelayMs = 1000, int maxAttempts = 10) {
            Init();
            if (!IsStart) {
                return;
            }

            _attemptConnectCts = new CancellationTokenSource();
            bool success = false;
            for (int i = 0; i < maxAttempts && !_attemptConnectCts.IsCancellationRequested; i++) {
                _connectTimeoutCts = new CancellationTokenSource(timeoutMs);
                success = await TryConnectOnce(_connectTimeoutCts.Token, addr, port);
                if (success) {
                    _addr = addr;
                    _port = port;
                    Debug.LogFormat("Connect {0}:{1} success", addr, port);
                    break;
                }

                Debug.LogFormat("Connect {0}:{1} attempt {2}/{3} failed", addr, port, i + 1, maxAttempts);

                try {
                    await Task.Delay(retryDelayMs, _attemptConnectCts.Token);
                } catch (OperationCanceledException) {
                    Debug.LogFormat("Connect {0}:{1} attempt {2}/{3} canceled", addr, port, i + 1, maxAttempts);
                }
            }

            if (!success) {
                Debug.LogErrorFormat("Connect {0}:{1} attempt {2} times all failed", addr, port, maxAttempts);
                Close();
            }
        }

        private async Task<bool> TryConnectOnce(CancellationToken token, string addr, int port) {
            try {
                _client = new TcpClient();
                Task connectTask = _client.ConnectAsync(addr, port);
                Task delayTask = Task.Delay(Timeout.Infinite, token);
                Task finished = await Task.WhenAny(connectTask, delayTask);
                if (finished != connectTask || !_client.Connected) {
                    _client.Close();
                    return false;
                }

                _stream = _client.GetStream();
                _cts = new CancellationTokenSource();
                _ = ListenLoopAsync(_cts.Token);
                _ = ParseLoopAsync(_cts.Token);
                return true;
            } catch (OperationCanceledException) {
                Debug.LogError("Connect cancelled by user");
                return false;
            } catch (Exception ex) {
                Debug.LogErrorFormat("Connect attempt failed: {0}", ex.Message);
                return false;
            }
        }

        public void Close() {
            _isClientClosing = true;
            UnRegisterAll();
            CloseTokenSource(ref _cts);
            CloseTokenSource(ref _connectTimeoutCts);
            CloseTokenSource(ref _attemptConnectCts);
            _stream?.Close();
            _client?.Close();
            IsStart = false;

            InvokeOnClose();
        }

        private void InvokeOnClose() {
            if (_hasClosed) {
                return;
            }

            _hasClosed = true;
            Debug.LogFormat("Connect {0}:{1} closed", _addr, _port);
            OnClose?.Invoke();
        }


        private static void CloseTokenSource(ref CancellationTokenSource cts) {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        public void SendMessage(ushort msgType, IMessage message) {
            byte[] bodyBytes = message.ToByteArray();
            byte[] lengthBytes = ((uint)bodyBytes.Length).GetBytesBigEndian();
            byte[] msgTypeBytes = msgType.GetBytesBigEndian();

            using MemoryStream mem = new MemoryStream();

            mem.Write(MagicNumberBytes, 0, 4);
            mem.Write(msgTypeBytes, 0, 2);
            mem.Write(lengthBytes, 0, 4);
            mem.Write(bodyBytes, 0, bodyBytes.Length);

            byte[] payload = mem.ToArray();
            _stream.Write(payload, 0, payload.Length);
        }

        private async Task ListenLoopAsync(CancellationToken ct) {
            byte[] temp = new byte[1024];
            try {
                while (!ct.IsCancellationRequested) {
                    int read = await _stream.ReadAsync(temp, 0, temp.Length, ct);
                    if (read == 0) {
                        throw new IOException("Disconnected");
                    }

                    _ringBufferStream.Push(temp, 0, read);
                }
            } catch (OperationCanceledException) {
                // 连接结束: 主动断开
            } catch (IOException e) {
                if (_isClientClosing) {
                    // 连接结束: 主动断开
                } else if (e.Message.Equals("Disconnected")) {
                    // 连接结束: 服务器断开连接
                    Close();
                } else {
                    // 连接结束
                    Debug.LogErrorFormat("连接结束: {0}", e);
                }
            } catch (Exception e) {
                // 连接异常
                Debug.LogErrorFormat("连接异常: {0}", e);
            } finally {
                // 连接结束
                // Debug.LogError("============ 连接结束");
                _isClientClosing = false;

                InvokeOnClose();
            }
        }

        private async Task ParseLoopAsync(CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                await ParseMagicNumber(ct);
                await ParseOther(ct);
            }
        }

        private async Task ParseMagicNumber(CancellationToken ct) {
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
        }

        private async Task ParseOther(CancellationToken ct) {
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
    }
}
