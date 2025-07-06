using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Proto {
    public static class NetworkStreamExtension {
        public static async Task<uint> ReadUint32(this NetworkStream stream, CancellationToken ct) {
            byte[] bytes = new byte[4];
            await ReadExactAsync(stream, bytes, 0, 4, ct);
            return bytes.ToUInt32BigEndian();
        }
        
        public static async Task<ushort> ReadUint16(this NetworkStream stream, CancellationToken ct) {
            byte[] bytes = new byte[2];
            await ReadExactAsync(stream, bytes, 0, 2, ct);
            return bytes.ToUInt16BigEndian();
        }

        private static async Task ReadExactAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken ct) {
            int readBytes = 0;
            while (readBytes < count) {
                int n = await stream.ReadAsync(buffer, offset + readBytes, count - readBytes, ct);
                if (n == 0) {
                    throw new IOException("Remote socket closed");
                }

                readBytes += n;
            }
        }
    }
}
