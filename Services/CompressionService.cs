using FileCompressorApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FileCompressorApp.Services
{
    public static class CompressionService
    {
        public static CompressionResult CompressToArchive(List<string> filePaths, string algorithm, string archiveOutputPath, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                token.ThrowIfCancellationRequested();

            var result = new CompressionResult
            {
                FileName = Path.GetFileName(archiveOutputPath),
                AlgorithmUsed = algorithm
            };

            try
            {
                using var archiveStream = new FileStream(archiveOutputPath, FileMode.Create);
                using var writer = new BinaryWriter(archiveStream);

                writer.Write(filePaths.Count);

                foreach (var filePath in filePaths)
                {
                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();

                    if (!File.Exists(filePath))
                        throw new FileNotFoundException($"الملف غير موجود: {filePath}");

                    string fileName = Path.GetFileName(filePath);
                    byte[] fileBytes = File.ReadAllBytes(filePath);

                    byte[] compressedBytes;

                    switch (algorithm)
                    {
                        case "Huffman":
                            compressedBytes = HuffmanCompressor.CompressBytes(fileBytes);
                            break;

                        case "Shannon-Fano":
                            compressedBytes = ShannonFanoCompressor.CompressBytes(fileBytes, token).CompressedData;
                            break;

                        default:
                            throw new ArgumentException("خوارزمية غير مدعومة");
                    }

                    writer.Write(fileName.Length);
                    writer.Write(fileName.ToCharArray());

                    writer.Write(algorithm); // إضافة نوع الخوارزمية
                    writer.Write(compressedBytes.Length);
                    writer.Write(compressedBytes);
                }

                result.OriginalSize = filePaths.Sum(f => new FileInfo(f).Length);
                result.CompressedSize = new FileInfo(archiveOutputPath).Length;

                File.AppendAllText("log.txt", $"تم ضغط أرشيف: {result.FileName} باستخدام: {algorithm}\n");
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }

            return result;
        }

        //=============================================================>

        public static void DecompressArchive(string archiveInputPath, string outputFolder, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(outputFolder))
                outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FileCompressorOutput");

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            using var archiveStream = new FileStream(archiveInputPath, FileMode.Open);
            using var reader = new BinaryReader(archiveStream);

            int fileCount = reader.ReadInt32();

            for (int i = 0; i < fileCount; i++)
            {
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                int fileNameLen = reader.ReadInt32();
                string fileName = new string(reader.ReadChars(fileNameLen));

                string algorithm = reader.ReadString();
                int compressedLength = reader.ReadInt32();
                byte[] compressedBytes = reader.ReadBytes(compressedLength);

                byte[] decompressedBytes = algorithm switch
                {
                    "Huffman" => HuffmanCompressor.DecompressBytes(compressedBytes),
                    "Shannon-Fano" => ShannonFanoCompressor.DecompressBytes(compressedBytes, token),
                    _ => throw new ArgumentException("خوارزمية غير معروفة")
                };

                string outputFilePath = Path.Combine(outputFolder, fileName);
                File.WriteAllBytes(outputFilePath, decompressedBytes);
            }
        }

        //=============================================================>
        public static void ExtractSingleFileFromArchive(string archiveInputPath, string outputFolder, string targetFileName, CancellationToken token)
        {
            using var archiveStream = new FileStream(archiveInputPath, FileMode.Open);
            using var reader = new BinaryReader(archiveStream);

            int fileCount = reader.ReadInt32();

            for (int i = 0; i < fileCount; i++)
            {
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                int fileNameLen = reader.ReadInt32();
                string fileName = new string(reader.ReadChars(fileNameLen));

                string algorithm = reader.ReadString();
                int compressedLength = reader.ReadInt32();

                if (fileName.Equals(targetFileName, StringComparison.OrdinalIgnoreCase))
                {
                    byte[] compressedBytes = reader.ReadBytes(compressedLength);

                    byte[] decompressedBytes = algorithm switch
                    {
                        "Huffman" => HuffmanCompressor.DecompressBytes(compressedBytes),
                        "Shannon-Fano" => ShannonFanoCompressor.DecompressBytes(compressedBytes, token),
                        _ => throw new ArgumentException("خوارزمية غير معروفة")
                    };

                    if (!Directory.Exists(outputFolder))
                        Directory.CreateDirectory(outputFolder);

                    string outputFilePath = Path.Combine(outputFolder, fileName);
                    File.WriteAllBytes(outputFilePath, decompressedBytes);

                    File.AppendAllText("log.txt", $"✅ تم استخراج الملف المفرد: {fileName} من الأرشيف\n");
                    return;
                }
                else
                {
                    // تخطي البيانات المضغوطة لهذا الملف لأنه ليس هو المطلوب
                    archiveStream.Seek(compressedLength, SeekOrigin.Current);
                }
            }

        

            throw new FileNotFoundException($"الملف المطلوب '{targetFileName}' غير موجود في الأرشيف.");
        }
    }
}
