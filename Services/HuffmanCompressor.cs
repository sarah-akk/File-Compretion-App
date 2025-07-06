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

        public static List<string> ListFilesInArchive(string archivePath)
        {
            var fileNames = new List<string>();

            using (var stream = new FileStream(archivePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(stream))
            {
                int fileCount = reader.ReadInt32();

                for (int i = 0; i < fileCount; i++)
                {
                    int fileNameLen = reader.ReadInt32();
                    var fileNameChars = reader.ReadChars(fileNameLen);
                    string fileName = new string(fileNameChars);

                    string algorithm = reader.ReadString(); 

                    int compressedLength = reader.ReadInt32();
                    fileNames.Add(fileName);

                    stream.Seek(compressedLength, SeekOrigin.Current);
                }
            }

            return fileNames;
        }

        //=============================================================>


        public static void ExtractSingleFile(string archivePath, string fileName, string outputPath)
        {
            using (var stream = new FileStream(archivePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(stream))
            {
                int fileCount = reader.ReadInt32();

                for (int i = 0; i < fileCount; i++)
                {
                    int fileNameLen = reader.ReadInt32();
                    var fileNameChars = reader.ReadChars(fileNameLen);
                    string currentFileName = new string(fileNameChars);

                    string algorithm = reader.ReadString(); // ✅ ضروري هنا

                    int compressedLength = reader.ReadInt32();

                    if (currentFileName == fileName)
                    {
                        byte[] compressedData = reader.ReadBytes(compressedLength);

                        byte[] decompressedData = algorithm switch
                        {
                            "Huffman" => HuffmanCompressor.DecompressBytes(compressedData),
                            "Shannon-Fano" => ShannonFanoCompressor.DecompressBytes(compressedData, CancellationToken.None),
                            _ => throw new ArgumentException("خوارزمية غير معروفة")
                        };

                        string outputFile = Path.Combine(outputPath, currentFileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(outputFile) ?? outputPath);
                        File.WriteAllBytes(outputFile, decompressedData);
                        return;
                    }
                    else
                    {
                        // تخطي بيانات الملف بالكامل
                        stream.Seek(compressedLength, SeekOrigin.Current);
                    }
                }

                throw new FileNotFoundException("الملف غير موجود في الأرشيف.");
            }
        }


        //=============================================================>


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

        //=============================================================>

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

        //=============================================================>

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

        //=============================================================>

        public static byte[] CompressBytes(byte[] fileBytes)
            {
                var freqTable = BuildFrequencyTable(fileBytes);
                var root = BuildHuffmanTree(freqTable);
                var codeTable = new Dictionary<byte, string>();
                BuildCodeTable(root, "", codeTable);

                var encodedBits = new StringBuilder();
                foreach (byte b in fileBytes)
                    encodedBits.Append(codeTable[b]);

                string bitString = encodedBits.ToString();
                var byteList = new List<byte>();
                for (int i = 0; i < bitString.Length; i += 8)
                {
                    string byteStr = bitString.Substring(i, Math.Min(8, bitString.Length - i));
                    if (byteStr.Length < 8)
                        byteStr = byteStr.PadRight(8, '0');
                    byteList.Add(Convert.ToByte(byteStr, 2));
                }

                using var ms = new MemoryStream();
                using var writer = new BinaryWriter(ms);

                // اكتب عدد الرموز
                writer.Write(codeTable.Count);
                foreach (var kvp in codeTable)
                {
                    writer.Write(kvp.Key);
                    writer.Write(kvp.Value);
                }

                // اكتب طول البتات الفعلية
                writer.Write(bitString.Length);

                // اكتب البيانات المضغوطة
                writer.Write(byteList.ToArray());

                writer.Flush();
                return ms.ToArray();
            }


        //=============================================================>


        // فك الضغط من بايتات مضغوطة (لتستخدمها في فك الأرشيف)
        public static byte[] DecompressBytes(byte[] compressedData)
            {
                using var ms = new MemoryStream(compressedData);
                using var reader = new BinaryReader(ms);

                int symbolCount = reader.ReadInt32();
                var codeTable = new Dictionary<string, byte>();
                for (int i = 0; i < symbolCount; i++)
                {
                    byte symbol = reader.ReadByte();
                    string code = reader.ReadString();
                    codeTable[code] = symbol;
                }

                int totalBits = reader.ReadInt32();
                var dataBytes = reader.ReadBytes((int)(ms.Length - ms.Position));

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

                return outputBytes.ToArray();
            }
        
    }
}
