using FileCompressorApp.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FileCompressorApp.Services
{
    public static class HuffmanCompressor
    {
        private class Node
        {
            public char? Symbol;
            public int Frequency;
            public Node Left;
            public Node Right;
        }

        private static Dictionary<char, int> BuildFrequencyTable(string text)
        {
            var freq = new Dictionary<char, int>();
            foreach (char c in text)
            {
                if (!freq.ContainsKey(c))
                    freq[c] = 0;
                freq[c]++;
            }
            return freq;
        }

        private static Node BuildHuffmanTree(Dictionary<char, int> freqTable)
        {
            var priorityQueue = new List<Node>();

            foreach (var kvp in freqTable)
            {
                priorityQueue.Add(new Node { Symbol = kvp.Key, Frequency = kvp.Value });
            }

            while (priorityQueue.Count > 1)
            {
                var ordered = priorityQueue.OrderBy(n => n.Frequency).ToList();
                var left = ordered[0];
                var right = ordered[1];

                var parent = new Node
                {
                    Symbol = null,
                    Frequency = left.Frequency + right.Frequency,
                    Left = left,
                    Right = right
                };

                priorityQueue.Remove(left);
                priorityQueue.Remove(right);
                priorityQueue.Add(parent);
            }

            return priorityQueue[0]; // root node
        }

        private static void BuildCodeTable(Node node, string code, Dictionary<char, string> codeTable)
        {
            if (node == null)
                return;

            if (node.Symbol != null)
            {
                codeTable[node.Symbol.Value] = code;
            }

            BuildCodeTable(node.Left, code + "0", codeTable);
            BuildCodeTable(node.Right, code + "1", codeTable);
        }

        public static void Compress(string inputFilePath, string outputFilePath, CancellationToken token)
        {

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);


            string text = File.ReadAllText(inputFilePath);

            var freqTable = BuildFrequencyTable(text);
            var root = BuildHuffmanTree(freqTable);
            var codeTable = new Dictionary<char, string>();
            BuildCodeTable(root, "", codeTable);

            var encodedText = new StringBuilder();
            foreach (char c in text)
            {
                encodedText.Append(codeTable[c]);
            }

            // Save compressed binary as string (for demonstration only)
            File.WriteAllText(outputFilePath, encodedText.ToString());

            // Save log
            var bytes = BitHelper.ConvertToBytes(encodedText.ToString());
            File.WriteAllBytes(outputFilePath, bytes);

        }



    }
}
