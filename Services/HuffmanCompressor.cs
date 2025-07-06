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
            public byte? Symbol;
            public int Frequency;
            public Node Left;
            public Node Right;
        }

        private static Dictionary<byte, int> BuildFrequencyTable(byte[] data)
        {
            var freq = new Dictionary<byte, int>();
            foreach (var b in data)
            {
                if (!freq.ContainsKey(b))
                    freq[b] = 0;
                freq[b]++;
            }
            return freq;
        }

        private static Node BuildHuffmanTree(Dictionary<byte, int> freqTable)
        {
            var queue = new List<Node>();
            foreach (var kvp in freqTable)
            {
                queue.Add(new Node { Symbol = kvp.Key, Frequency = kvp.Value });
            }

            while (queue.Count > 1)
            {
                var ordered = queue.OrderBy(n => n.Frequency).ToList();
                var left = ordered[0];
                var right = ordered[1];

                var parent = new Node
                {
                    Frequency = left.Frequency + right.Frequency,
                    Left = left,
                    Right = right
                };

                queue.Remove(left);
                queue.Remove(right);
                queue.Add(parent);
            }

            return queue[0];
        }

        private static void BuildCodeTable(Node node, string code, Dictionary<byte, string> codeTable)
        {
            if (node == null) return;

            if (node.Symbol != null)
            {
                codeTable[node.Symbol.Value] = code;
            }

            BuildCodeTable(node.Left, code + "0", codeTable);
            BuildCodeTable(node.Right, code + "1", codeTable);
        }

        public static void Compress(string inputPath, string outputPath, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            byte[] fileBytes = File.ReadAllBytes(inputPath);

            var freqTable = BuildFrequencyTable(fileBytes);
            var root = BuildHuffmanTree(freqTable);
            var codeTable = new Dictionary<byte, string>();
            BuildCodeTable(root, "", codeTable);

            var encodedBits = new StringBuilder();
            foreach (byte b in fileBytes)
            {
                encodedBits.Append(codeTable[b]);
            }

            // Convert bit string to byte[]
            var bitString = encodedBits.ToString();
            var byteList = new List<byte>();
            for (int i = 0; i < bitString.Length; i += 8)
            {
                string byteStr = bitString.Substring(i, Math.Min(8, bitString.Length - i));
                if (byteStr.Length < 8)
                    byteStr = byteStr.PadRight(8, '0'); // pad with zeros
                byteList.Add(Convert.ToByte(byteStr, 2));
            }

            using var stream = new BinaryWriter(File.Open(outputPath, FileMode.Create));

            // Write header: number of symbols
            stream.Write(codeTable.Count);
            foreach (var kvp in codeTable)
            {
                stream.Write(kvp.Key);                // Symbol (byte)
                stream.Write(kvp.Value);              // Code string
            }

            // Write bit length
            stream.Write(bitString.Length); // actual number of bits used (not padded)

            // Write data
            stream.Write(byteList.ToArray());
        }






        public static void Decompress(string inputPath, string outputFolder)
        {
            // إذا لم يتم تحديد مسار أو مسار فارغ، استخدم مجلد مؤقت
            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FileCompressorOutput");
            }

            try
            {
                // تأكد أن المجلد موجود أو أنشئه
                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                using var reader = new BinaryReader(File.OpenRead(inputPath));

                int symbolCount = reader.ReadInt32();
                var codeTable = new Dictionary<string, byte>();
                for (int i = 0; i < symbolCount; i++)
                {
                    byte symbol = reader.ReadByte();
                    string code = reader.ReadString();
                    codeTable[code] = symbol;
                }

                int totalBits = reader.ReadInt32();
                var dataBytes = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));

                var bitStr = new StringBuilder();
                foreach (byte b in dataBytes)
                    bitStr.Append(Convert.ToString(b, 2).PadLeft(8, '0'));

                string bits = bitStr.ToString().Substring(0, totalBits);

                var current = "";
                var outputBytes = new List<byte>();

                foreach (char c in bits)
                {
                    current += c;
                    if (codeTable.ContainsKey(current))
                    {
                        outputBytes.Add(codeTable[current]);
                        current = "";
                    }
                }

                // احفظ الملف في المجلد بعد التأكد من صلاحيات الكتابة
                string outputFilePath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(inputPath) + "_decompressed.bin");
                File.WriteAllBytes(outputFilePath, outputBytes.ToArray());

                Console.WriteLine($"تم الحفظ في: {outputFilePath}");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("خطأ: لا تملك صلاحية الكتابة في هذا المجلد.");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("خطأ: " + ex.Message);
                throw;
            }
        }
    }
}
