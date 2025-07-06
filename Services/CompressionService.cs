using FileCompressorApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
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
                var combinedText = new StringBuilder();

                foreach (var file in filePaths)
                {
                    string content = File.ReadAllText(file);
                    combinedText.AppendLine($"###FILE:{Path.GetFileName(file)}###");
                    combinedText.Append(content);
                }

                var tempPath = Path.GetTempFileName(); // Temporarily save merged content
                File.WriteAllText(tempPath, combinedText.ToString());
                result.OriginalSize = new FileInfo(tempPath).Length;

                switch (algorithm)
                {
                    case "Huffman":
                        HuffmanCompressor.Compress(tempPath, archiveOutputPath, token);
                        break;

                    case "Shannon-Fano":
                        ShannonFanoCompressor.Compress(tempPath, archiveOutputPath, token);
                        break;

                    default:
                        throw new ArgumentException("خوارزمية غير مدعومة");
                }

                result.CompressedSize = new FileInfo(archiveOutputPath).Length;
                File.AppendAllText("log.txt", $"تم ضغط أرشيف: {result.FileName} باستخدام: {algorithm}\n");
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }

            return result;
        }


    }
}