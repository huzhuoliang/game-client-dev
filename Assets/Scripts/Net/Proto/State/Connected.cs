using System;
using System.Buffers;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Util;
using GameServerServices.MessageType;
using Google.Protobuf;
using UnityEngine;

namespace Net.Proto.State {
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class Connected : TcpClientStateBase {
        private const int MAGIC_NUMBER_SIZE = 4;
        private const uint MAGIC_NUMBER = 0xCAFEBABE;
        private const int MAX_BODY_SIZE = 1024 * 1024;

        private static readonly byte[] sMagicNumberBytes = MAGIC_NUMBER.GetBytesBigEndian();
        private static readonly ArrayPool<byte> sBufferPool = ArrayPool<byte>.Shared;

        private Connected() { }

        protected override async UniTask<TcpClientStateBase> RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            UniTask listen = ListenLoopAsync(ctx, linkedCts.Token).Preserve();
            UniTask parse  = ParseLoopAsync(ctx, linkedCts.Token).Preserve();

            try {
                int idx = await UniTask.WhenAny(listen, parse);
                // 让先完成的循环把异常抛出来（如果有）
                if (idx == 0) {
                    await listen;
                } else {
                    await parse;
                }
            } catch (OperationCanceledException) {
                // 顶层 ct 取消 → FSM 整体退出，由 StartAsync 顶层 catch 处理
                throw;
            } catch (Exception e) {
                // socket 断开 / IOException / 协议错误 → 走到 Disconnecting
                Debug.LogFormat("Connection broken: {0}", e.Message);
            } finally {
                // 不管哪条循环先停，把另一条一起叫停，再 drain 一次避免泄漏
                linkedCts.Cancel();
                try { await listen; } catch { /* expected on cleanup */ }
                try { await parse; }  catch { /* expected on cleanup */ }
            }

            return GetInstance<Disconnecting>();
        }

        private static async UniTask ListenLoopAsync(ITcpClientFSMCtx ctx, CancellationToken ct) {
            Stream stream = ctx.NetworkStream;
            RingBufferStream ring = ctx.RingBuffer;
            byte[] temp = new byte[1024];
            while (!ct.IsCancellationRequested) {
                int read = await stream.ReadAsync(temp, 0, temp.Length, ct).AsUniTask();
                if (read == 0) {
                    throw new IOException("Disconnected");
                }
                ring.Push(temp, 0, read);
            }
        }

        private static async UniTask ParseLoopAsync(ITcpClientFSMCtx ctx, CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                await ParseMagicNumber(ctx, ct);
                await ParseOther(ctx, ct);
            }
        }

        private static async UniTask ParseMagicNumber(ITcpClientFSMCtx ctx, CancellationToken ct) {
            RingBufferStream ring = ctx.RingBuffer;
            int matched = 0;
            while (matched < MAGIC_NUMBER_SIZE && !ct.IsCancellationRequested) {
                byte b = await ring.ReadByteAsync(ct);
                if (b == sMagicNumberBytes[matched]) {
                    matched++;
                } else {
                    matched = b == sMagicNumberBytes[0] ? 1 : 0;
                }
            }
        }

        private static async UniTask ParseOther(ITcpClientFSMCtx ctx, CancellationToken ct) {
            RingBufferStream ring = ctx.RingBuffer;
            ushort msgType = await ring.ReadUint16(ct);
            MessageType messageType = (MessageType)msgType;
            int length = (int)await ring.ReadUint32(ct);
            if (length > MAX_BODY_SIZE) {
                Debug.LogErrorFormat("Length too large: {0}", length);
                return;
            }

            byte[] dataBuffer = sBufferPool.Rent(length);
            try {
                await ring.ReadBytesAsync(dataBuffer, 0, length, ct);
                uint crc = await ring.ReadUint32(ct);
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
    }
}
