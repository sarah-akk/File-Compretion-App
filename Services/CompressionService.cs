using FileCompressorApp.Models;
using System;
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
        public static CompressionResult CompressToArchive(List<string> filePaths, string algorithm, string archiveOutputPath, CancellationToken token , string? password = null)
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

                    writer.Write(string.IsNullOrEmpty(password) ? 0 : password.Length);
                    if (!string.IsNullOrEmpty(password))
                        writer.Write(password.ToCharArray());

                    writer.Write(algorithm);
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

        public static void DecompressArchive(string archiveInputPath, string outputFolder, CancellationToken token, IProgress<int>? progress = null, string? userInputPassword = null)
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
                    return;

                int fileNameLen = reader.ReadInt32();
                string fileName = new string(reader.ReadChars(fileNameLen));

                int passwordLen = reader.ReadInt32();
                string? archivePassword = passwordLen > 0 ? new string(reader.ReadChars(passwordLen)) : null;

                if (!string.IsNullOrEmpty(archivePassword))
                {
                    if (archivePassword != userInputPassword)
                    {
                        System.Windows.MessageBox.Show(
                            $"❌ كلمة السر غير صحيحة.\n\n" ,
                            "خطأ في كلمة السر",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                        return;
                    }
                }


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
                progress?.Report((i + 1) * 100 / fileCount);

            }
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
