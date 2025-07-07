using FileCompressorApp.Helpers;
using FileCompressorApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FileCompressorApp.Services
{
        public class CompressedResult
    {
        public byte[] CompressedData { get; set; }
        public int OriginalSize { get; set; }
    }
    //=============================================================>

    public static class ShannonFanoCompressor
    {
        private class SymbolCode
        {
            public byte Symbol;
            public int Frequency;
            public string Code = "";
        }
        //=============================================================>

        private static List<SymbolCode> BuildFrequencyTable(byte[] data)
        {
            var freqDict = new Dictionary<byte, int>();

            foreach (byte b in data)
            {
                if (!freqDict.ContainsKey(b))
                    freqDict[b] = 0;
                freqDict[b]++;
            }

            return freqDict
                .Select(kvp => new SymbolCode { Symbol = kvp.Key, Frequency = kvp.Value })
                .OrderByDescending(sc => sc.Frequency)
                .ToList();
        }

        //=============================================================>

        private static void BuildShannonFanoCodes(List<SymbolCode> symbols, int start, int end)
        {
            if (start >= end)
                return;

            int total = symbols.Skip(start).Take(end - start + 1).Sum(s => s.Frequency);
            int halfTotal = total / 2;

            int split = start;
            int sum = 0;

            for (int i = start; i <= end; i++)
            {
                sum += symbols[i].Frequency;
                if (sum >= halfTotal)
                {
                    split = i;
                    break;
                }
            }

            for (int i = start; i <= split; i++)
                symbols[i].Code += "0";

            for (int i = split + 1; i <= end; i++)
                symbols[i].Code += "1";

            BuildShannonFanoCodes(symbols, start, split);
            BuildShannonFanoCodes(symbols, split + 1, end);
        }
        //=============================================================>

        public static CompressedResult CompressBytes(byte[] inputData, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            var symbols = BuildFrequencyTable(inputData);
            BuildShannonFanoCodes(symbols, 0, symbols.Count - 1);

            var codeTable = symbols.ToDictionary(s => s.Symbol, s => s.Code);

            var encoded = new StringBuilder();
            foreach (var b in inputData)
            {
                encoded.Append(codeTable[b]);
            }

            var compressedData = BitHelper.ConvertToBytes(encoded.ToString());

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write(codeTable.Count);
            foreach (var kvp in codeTable)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }

            writer.Write(encoded.Length);
            writer.Write(compressedData);

            return new CompressedResult
            {
                CompressedData = ms.ToArray(),
                OriginalSize = inputData.Length
            };
        }
        //=============================================================>

        public static byte[] DecompressBytes(byte[] compressedBytes, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            using var ms = new MemoryStream(compressedBytes);
            using var reader = new BinaryReader(ms);

            int tableSize = reader.ReadInt32();
            var codeTable = new Dictionary<string, byte>();

            for (int i = 0; i < tableSize; i++)
            {
                byte symbol = reader.ReadByte();
                string code = reader.ReadString();
                codeTable[code] = symbol;
            }

            int bitLength = reader.ReadInt32();
            byte[] encodedData = reader.ReadBytes((bitLength + 7) / 8);
            string bitString = BitHelper.ConvertToBitString(encodedData, bitLength);

            var output = new List<byte>();
            var currentBits = new StringBuilder();

            foreach (char bit in bitString)
            {
                currentBits.Append(bit);
                if (codeTable.TryGetValue(currentBits.ToString(), out byte b))
                {
                    output.Add(b);
                    currentBits.Clear();
                }
            }

            return output.ToArray();
        }
    }
}
