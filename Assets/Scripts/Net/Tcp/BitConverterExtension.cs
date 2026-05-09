using System;
using UnityEngine;

namespace Net.Proto {
    public static class BitConverterExtension {
        public static byte[] GetBytesBigEndian(this uint value) {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        public static byte[] GetBytesBigEndian(this ushort value) {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        public static uint ToUInt32BigEndian(this byte[] value) {
            if (value.Length < 4) {
                Debug.LogErrorFormat("Byte array size is {0} too small. Expected {1}.", value.Length, 4);
                return 0;
            }

            if (BitConverter.IsLittleEndian) {
                Array.Reverse(value, 0, 4);
            }

            return BitConverter.ToUInt32(value);
        }
        
        public static ushort ToUInt16BigEndian(this byte[] value) {
            if (value.Length < 2) {
                Debug.LogErrorFormat("Byte array size is {0} too small. Expected {1}.", value.Length, 2);
                return 0;
            }

            if (BitConverter.IsLittleEndian) {
                Array.Reverse(value, 0, 2);
            }

            return BitConverter.ToUInt16(value);
        }
    }
}
