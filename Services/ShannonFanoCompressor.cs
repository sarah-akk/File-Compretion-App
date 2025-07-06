using FileCompressorApp.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace FileCompressorApp.Services
{
    public static class ShannonFanoCompressor
    {
        private class SymbolCode
        {
            public char Symbol;
            public int Frequency;
            public string Code = "";
        }

        private static List<SymbolCode> BuildFrequencyTable(string text)
        {
            var freqDict = new Dictionary<char, int>();

            foreach (char c in text)
            {
                if (!freqDict.ContainsKey(c))
                    freqDict[c] = 0;
                freqDict[c]++;
            }

            return freqDict
                .Select(kvp => new SymbolCode { Symbol = kvp.Key, Frequency = kvp.Value })
                .OrderByDescending(sc => sc.Frequency)
                .ToList();
        }

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

        public static void Compress(string inputFilePath, string outputFilePath, CancellationToken token)
        {

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);


            string text = File.ReadAllText(inputFilePath);

            var symbols = BuildFrequencyTable(text);
            BuildShannonFanoCodes(symbols, 0, symbols.Count - 1);

            var codeTable = symbols.ToDictionary(s => s.Symbol, s => s.Code);

            var encoded = new StringBuilder();
            foreach (char c in text)
            {
                encoded.Append(codeTable[c]);
            }

            var bytes = BitHelper.ConvertToBytes(encoded.ToString());
            File.WriteAllBytes(outputFilePath, bytes);

            File.AppendAllText("log.txt", $"تم ضغط {Path.GetFileName(inputFilePath)} باستخدام Shannon-Fano\n");
        }
    }
}

