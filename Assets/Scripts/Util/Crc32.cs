namespace Game.Util {
    public static class Crc32 {
        private static readonly uint[] table;

        static Crc32() {
            const uint poly = 0xEDB88320u;
            table = new uint[256];
            for (uint i = 0; i < table.Length; i++) {
                uint crc = i;
                for (int j = 0; j < 8; j++) {
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ poly : crc >> 1;
                }

                table[i] = crc;
            }
        }

        public static uint Compute(byte[] buffer, int offset, int length) {
            uint crc = 0xFFFFFFFFu;

            for (int i = offset; i < offset + length; i++) {
                byte index = (byte)((crc ^ buffer[i]) & 0xFF);
                crc = (crc >> 8) ^ table[index];
            }

            return ~crc; // same as Go's ChecksumIEEE
        }

        public static uint Compute(byte[] buffer) {
            return Compute(buffer, 0, buffer.Length);
        }
    }
}
