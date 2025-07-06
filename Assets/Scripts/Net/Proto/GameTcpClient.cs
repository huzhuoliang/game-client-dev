using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GameServerServices.MessageType;
using Google.Protobuf;
using Net.Tcp;
using UnityEngine;

namespace Net.Proto {
    public class GameTcpClient {
        private const int MAGIC_NUMBER_SIZE = 4;
        private const int TYPE_SIZE = 2;
        private const int LENGTH_SIZE = 4;
        private const int CRC_SIZE = 4;

        private const uint MAGIC_NUMBER = 0xCAFEBABE;

        private byte[] _magicNumberBytes;

        private byte[] MagicNumberBytes => _magicNumberBytes ??= MAGIC_NUMBER.GetBytesBigEndian();

        private string _addr = "192.168.50.16";
        private int _port = 50052;

        /// <summary>
        /// [Magic] [Type] [Length] [CRC32] [Body]
        /// </summary>
        private const int HEADER_SIZE = MAGIC_NUMBER_SIZE + TYPE_SIZE + LENGTH_SIZE; /* TODO Add CRC32 */
        // private const int HEADER_SIZE2 = MAGIC_NUMBER_SIZE + TYPE_SIZE + LENGTH_SIZE + CRC_SIZE;

        private const int MAX_BODY_SIZE = 1024 * 1024; // 1 MB

        private bool _init;

        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;

        private readonly RingBufferStream _ringBufferStream = new();

        private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

        public GameTcpClient SetAddr(string addr) {
            _addr = addr;
            return this;
        }

        public GameTcpClient SetPort(int port) {
            _port = port;
            return this;
        }

        public void Init() {
            RegisterAll();
            _init = true;
        }

        private static void RegisterAll() {
#if UNITY_EDITOR
            RegisterAllByReflection();
#else
            /* TODO 调用自动生成的类来注册 */
            // MessageHandlerRegistry.Instance.Register(new TcpHelloWorldHandler());
#endif
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
                    Debug.Log($"[AutoRegister] Registered {type.Name}");
                }
            }
        }

        public async Task StartConnectionAsync() {
            if (!_init) {
                return;
            }

            Debug.LogError("Start connection");
            _client = new TcpClient();
            await _client.ConnectAsync(_addr, _port);
            if (_client.Connected) {
                _stream = _client.GetStream();
                _cts = new CancellationTokenSource();
                _ = ListenLoopAsync(_cts.Token);
                _ = ParseLoopAsync(_cts.Token);
            }

            Debug.LogError("Connection success");
        }

        public bool Connected => _client?.Connected ?? false;

        public void Close() {
            _cts?.Cancel();
            _stream?.Close();
            _client?.Close();
            _init = false;
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
            while (!ct.IsCancellationRequested) {
                int read = await _stream.ReadAsync(temp, 0, temp.Length, ct);
                if (read == 0) {
                    throw new IOException("Disconnected");
                }

                _ringBufferStream.Push(temp, 0, read);
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
            uint length = await _ringBufferStream.ReadUint32(ct);
            if (length > MAX_BODY_SIZE) {
                Debug.LogErrorFormat("Length too large: {0}", length);
                return;
            }

            byte[] dataBuffer = _bufferPool.Rent((int)length);

            try {
                await _ringBufferStream.ReadBytesAsync(dataBuffer, 0, length, ct);

                if (!MessageHandlerRegistry.Instance.TryGetHandler(messageType, out var wrapper)) {
                    Debug.LogErrorFormat("[Client] Unknown messageType={0}", messageType);
                    return;
                }

                IMessage msg = wrapper.CreateMessage();
                msg.MergeFrom(new CodedInputStream(dataBuffer, 0, (int)length));
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
