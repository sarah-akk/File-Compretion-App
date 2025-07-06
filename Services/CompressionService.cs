using System;
using System.Collections.Generic;
using System.IO;
using FileCompressorApp.Models;

namespace FileCompressorApp.Services
{
    public static class CompressionService
    {
        public static List<CompressionResult> Compress(List<string> filePaths, string algorithm, CancellationToken token)
        {
            var results = new List<CompressionResult>();

            foreach (var file in filePaths)
            {
                if (token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                }

                var result = new CompressionResult
                {
                    FileName = Path.GetFileName(file),
                    AlgorithmUsed = algorithm
                };

                try
                {
                    var outputFile = $"{file}.compressed";
                    result.OriginalSize = new FileInfo(file).Length;

                    switch (algorithm)
                    {
                        case "Huffman":
                            HuffmanCompressor.Compress(file, outputFile, token);
                            break;

                        case "Shannon-Fano":
                            ShannonFanoCompressor.Compress(file, outputFile, token);
                            break;

                        default:
                            throw new ArgumentException("خوارزمية غير مدعومة");
                    }

                    result.CompressedSize = new FileInfo(outputFile).Length;
                    File.AppendAllText("log.txt", $"ضغط الملف: {result.FileName} باستخدام: {algorithm}\n");
                }
                catch (Exception ex)
                {
                    result.Error = ex.Message;
                }

                results.Add(result);
            }

            return results;
        }


    }
}