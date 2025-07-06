using System;

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
    }
}
