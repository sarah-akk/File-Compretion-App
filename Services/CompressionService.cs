using FileCompressorApp.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace FileCompressorApp.Services
{
    public static class CompressionService

    {

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

                    int passwordLength = reader.ReadInt32();
                    if (passwordLength > 0)
                    {
                        reader.ReadChars(passwordLength);
                    }

                    string algorithm = reader.ReadString();

                    int compressedLength = reader.ReadInt32();
                    stream.Seek(compressedLength, SeekOrigin.Current);

                    fileNames.Add(fileName);
                }
            }

            return fileNames;
        }

        //=============================================================>

        public static CompressionResult CompressToArchive(List<string> filePaths, string algorithm, string archiveOutputPath, CancellationToken token, string? password = null , PauseToken? pauseToken = null)
        {
            if (token.IsCancellationRequested)
                token.ThrowIfCancellationRequested();
            pauseToken?.WaitIfPaused();

            var result = new CompressionResult
            {
                FileName = Path.GetFileName(archiveOutputPath),
                AlgorithmUsed = algorithm
            };

            var compressedEntries = new ConcurrentBag<(string FileName, byte[] Data, string Algorithm, string? Password)>();

            try
            {
                Parallel.ForEach(filePaths, new ParallelOptions { CancellationToken = token }, filePath =>
                {
                    if (!File.Exists(filePath))
                        throw new FileNotFoundException($"الملف غير موجود: {filePath}");

                    pauseToken?.WaitIfPaused();

                    string fileName = Path.GetFileName(filePath);
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    byte[] compressed;

                    switch (algorithm)
                    {
                        case "Huffman":
                            compressed = HuffmanCompressor.CompressBytes(fileBytes);
                            break;
                        case "Shannon-Fano":
                            compressed = ShannonFanoCompressor.CompressBytes(fileBytes, token).CompressedData;
                            break;
                        default:
                            throw new ArgumentException("خوارزمية غير مدعومة");
                    }

                    compressedEntries.Add((fileName, compressed, algorithm, password));
                });

                using var archiveStream = new FileStream(archiveOutputPath, FileMode.Create);
                using var writer = new BinaryWriter(archiveStream);

                writer.Write(compressedEntries.Count);

                foreach (var entry in compressedEntries)
                {
                    writer.Write(entry.FileName.Length);
                    writer.Write(entry.FileName.ToCharArray());

                    writer.Write(string.IsNullOrEmpty(entry.Password) ? 0 : entry.Password.Length);
                    if (!string.IsNullOrEmpty(entry.Password))
                        writer.Write(entry.Password.ToCharArray());

                    writer.Write(entry.Algorithm);
                    writer.Write(entry.Data.Length);
                    writer.Write(entry.Data);
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

        public static void DecompressArchive(string archiveInputPath, string outputFolder, CancellationToken token, IProgress<int>? progress = null, string? userInputPassword = null)
        {
            if (string.IsNullOrWhiteSpace(outputFolder))
                outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FileCompressorOutput");

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            List<Task> tasks = new();
            List<string> fileNames = new();

            using var archiveStream = new FileStream(archiveInputPath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(archiveStream);

            int fileCount = reader.ReadInt32();
            var entries = new List<(string FileName, string Algorithm, string? Password, byte[] Data)>();

            for (int i = 0; i < fileCount; i++)
            {
                if (token.IsCancellationRequested) return;

                int fileNameLen = reader.ReadInt32();
                string fileName = new string(reader.ReadChars(fileNameLen));
                int passwordLen = reader.ReadInt32();
                string? archivePassword = passwordLen > 0 ? new string(reader.ReadChars(passwordLen)) : null;

                string algorithm = reader.ReadString();
                int compressedLength = reader.ReadInt32();
                byte[] compressedBytes = reader.ReadBytes(compressedLength);

                // كلمة السر
                if (!string.IsNullOrEmpty(archivePassword) && archivePassword != userInputPassword)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Windows.MessageBox.Show($"❌ كلمة السر غير صحيحة.\n\n", "خطأ في كلمة السر", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    return;
                }

                entries.Add((fileName, algorithm, archivePassword, compressedBytes));
            }

            int completed = 0;

            foreach (var entry in entries)
            {
                var task = Task.Run(() =>
                {
                    byte[] decompressedBytes = entry.Algorithm switch
                    {
                        "Huffman" => HuffmanCompressor.DecompressBytes(entry.Data),
                        "Shannon-Fano" => ShannonFanoCompressor.DecompressBytes(entry.Data, token),
                        _ => throw new ArgumentException("خوارزمية غير معروفة")
                    };

                    string outputFilePath = Path.Combine(outputFolder, entry.FileName);
                    File.WriteAllBytes(outputFilePath, decompressedBytes);

                    Interlocked.Increment(ref completed);
                    progress?.Report(completed * 100 / fileCount);
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
        }

        //=============================================================>

        public static void ExtractSingleFile(string archivePath, string fileName, string outputPath, string? userPassword = null)
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

                    int passwordLen = reader.ReadInt32();
                    string? archivePassword = passwordLen > 0 ? new string(reader.ReadChars(passwordLen)) : null;

                    // تحقق من كلمة السر لهذا الملف فقط
                    if (!string.IsNullOrEmpty(archivePassword))
                    {
                        if (userPassword != archivePassword)
                        {
                            System.Windows.MessageBox.Show(
                                $"❌ كلمة السر غير صحيحة.\n\n" ,
                                "خطأ في كلمة السر ",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                            );
                            return;
                        }
                    }

                    string algorithm = reader.ReadString();
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
                        // تخطي بيانات الملف غير المطابق
                        stream.Seek(compressedLength, SeekOrigin.Current);
                    }
                }

                throw new FileNotFoundException("الملف غير موجود في الأرشيف.");
            }
        }

        //=============================================================>
    }
}
