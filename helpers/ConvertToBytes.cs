using System;
using System.Text;

namespace FileCompressorApp.Helpers
{
    public static class BitHelper
    {
        public static byte[] ConvertToBytes(string bitString)
        {
            int numOfBytes = (bitString.Length + 7) / 8;
            byte[] bytes = new byte[numOfBytes];

            for (int i = 0; i < bitString.Length; i++)
            {
                if (bitString[i] == '1')
                {
                    bytes[i / 8] |= (byte)(1 << (7 - (i % 8)));
                }
            }

            return bytes;
        }

        public static string ConvertToBitString(byte[] bytes, int bitLength)
        {
            var sb = new StringBuilder(bitLength);

            for (int i = 0; i < bitLength; i++)
            {
                int byteIndex = i / 8;
                int bitIndex = 7 - (i % 8);
                bool bit = (bytes[byteIndex] & (1 << bitIndex)) != 0;
                sb.Append(bit ? '1' : '0');
            }

            return sb.ToString();
        }

        public static  byte[] ConvertBitsToBytes(string bits)
        {
            int byteCount = (bits.Length + 7) / 8;
            byte[] bytes = new byte[byteCount];
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i] == '1')
                {
                    bytes[i / 8] |= (byte)(1 << (7 - (i % 8)));
                }
            }
            return bytes;
        }

    }
}
