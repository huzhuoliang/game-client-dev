using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Net.Tcp {
    public class RingBufferStream : IDisposable {
        private readonly byte[] _buffer;
        private readonly byte[] _tmpBuffer = new byte[1024];
        private int _readPos;
        private int _writePos;
        private int _count;
        private readonly int _capacity;

        private readonly SemaphoreSlim _dataAvailable = new(0);

        public RingBufferStream(int capacity = 8192) {
            _capacity = capacity;
            _buffer = new byte[_capacity];
        }

        public int Length => _count;

        public void Push(byte[] data, int offset, int length) {
            if (_count + length > _capacity)
                throw new InvalidOperationException("Buffer overflow (fixed size)");

            for (int i = 0; i < length; i++) {
                _buffer[_writePos] = data[offset + i];
                _writePos = (_writePos + 1) % _capacity;
            }

            Interlocked.Add(ref _count, length);
            _dataAvailable.Release(length);
        }

        public async UniTask<byte> ReadByteAsync(CancellationToken ct) {
            await _dataAvailable.WaitAsync(ct).AsUniTask();

            byte value = _buffer[_readPos];
            _readPos = (_readPos + 1) % _capacity;
            Interlocked.Decrement(ref _count);
            return value;
        }

        public async UniTask PeekBytesAsync(byte[] buffer, int offset, int count, CancellationToken ct) {
            if (count <= 0) {
                return;
            }

            if (count > _capacity) {
                throw new ArgumentOutOfRangeException(nameof(count), "Peek count exceeds buffer capacity.");
            }

            while (_count < count) {
                await _dataAvailable.WaitAsync(ct).AsUniTask();
            }

            int firstPart = Math.Min(count, _capacity - _readPos);
            Array.Copy(_buffer, _readPos, buffer, offset, firstPart);
            int remaining = count - firstPart;
            if (remaining > 0) {
                Array.Copy(_buffer, 0, buffer, offset + firstPart, remaining);
            }
        }

        public async UniTask ReadBytesAsync(byte[] buffer, int offset, int count, CancellationToken ct) {
            if (count <= 0) {
                return;
            }

            for (int i = 0; i < count; i++) {
                buffer[offset + i] = await ReadByteAsync(ct);
            }
        }

        public int TryPeek(int offset, out byte value) {
            if (offset >= _count) {
                value = 0;
                return 0;
            }

            int pos = (_readPos + offset) % _capacity;
            value = _buffer[pos];
            return 1;
        }

        public async UniTask<uint> ReadUint32(CancellationToken ct) {
            await ReadBytesAsync(_tmpBuffer, 0, 4, ct);
            return _tmpBuffer.ToUInt32BigEndian();
        }

        public async UniTask<ushort> ReadUint16(CancellationToken ct) {
            await ReadBytesAsync(_tmpBuffer, 0, 2, ct);
            return _tmpBuffer.ToUInt16BigEndian();
        }

        public void Dispose() {
            _dataAvailable?.Dispose();
        }
    }
}
