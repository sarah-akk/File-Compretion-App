using FileCompressorApp;
using FileCompressorApp.Services;
using System;
using System.Windows;

namespace File_Compretion_App
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // تحقق هل هناك معاملات
            if (e.Args.Length == 2)
            {
                string command = e.Args[0];
                string path = e.Args[1].Trim('"');


                if (command == "-compress")
                {
                    CompressFileOrFolder(path);
                    Shutdown();  // أغلق التطبيق بعد الانتهاء
                    return;
                }
                else if (command == "-decompress")
                {
                    DecompressFile(path);
                    Shutdown();
                    return;
                }
            }

            // لا توجد معاملات أو معاملات غير معروفة => افتح النافذة الرئيسية
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private void CompressFileOrFolder(string path)
        {

            try
            {
                var filesToCompress = new List<string>();

                if (System.IO.File.Exists(path))
                {
                    filesToCompress.Add(path);
                }
                else if (System.IO.Directory.Exists(path))
                {
                    filesToCompress.AddRange(System.IO.Directory.GetFiles(path));
                }
                else
                {
                    System.Windows.MessageBox.Show("المسار غير صحيح.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = CompressionService.CompressToArchive(filesToCompress, "Huffman", "archive.huf", CancellationToken.None,null);

                System.Windows.MessageBox.Show($"تم ضغط {filesToCompress.Count} ملف بنجاح.\nالحجم الأصلي: {result.OriginalSize} بايت\nالحجم بعد الضغط: {result.CompressedSize} بايت",
                                "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ أثناء الضغط: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DecompressFile(string archivePath)
        {
            try
            {
                string outputFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FileCompressorOutput");

                CompressionService.DecompressArchive(archivePath, outputFolder, CancellationToken.None);

                System.Windows.MessageBox.Show($"تم فك ضغط الأرشيف بنجاح.\nتم الحفظ في: {outputFolder}", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ أثناء فك الضغط: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
