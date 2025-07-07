using FileCompressorApp.Services;
using System.Windows;
using System.Windows.Controls;

namespace FileCompressorApp
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource _cts;

        public MainWindow()
        {
            InitializeComponent();
        }

        ////=============================================================>

        private async void ExtractArchive_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Huffman Archive (*.huf)|*.huf"
            };

            if (dialog.ShowDialog() == true)
            {
                string archivePath = dialog.FileName;

                var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
                var result = folderDialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string outputFolder = folderDialog.SelectedPath;
                    try
                    {
                        ExtractionResultsListBox.Items.Clear();
                        DecompressionProgressBar.Visibility = Visibility.Visible;
                        DecompressionProgressBar.Value = 0;

                        CancelDecompressionButton.Visibility = Visibility.Visible;
                        _cts = new CancellationTokenSource();

                        var progress = new Progress<int>(percent =>
                        {
                            DecompressionProgressBar.Value = percent;
                        });

                        await Task.Run(() =>
                        {
                            CompressionService.DecompressArchive(archivePath, outputFolder, _cts.Token , progress);
                        });

                        var fileNames = HuffmanCompressor.ListFilesInArchive(archivePath);
                        foreach (var name in fileNames)
                        {
                            ExtractionResultsListBox.Items.Add($"✅ تم استخراج: {name}");
                        }

                        System.Windows.MessageBox.Show("تم فك الضغط بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        DecompressionProgressBar.Visibility = Visibility.Collapsed;
                        CancelDecompressionButton.Visibility = Visibility.Collapsed;
                        _cts?.Dispose();
                        _cts = null;
                    }
                }
            }
        }

        ////=============================================================>
        private void ExtractSingleFileFromArchive_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Huffman Archive (*.huf)|*.huf"
            };

            if (dialog.ShowDialog() == true)
            {
                string archivePath = dialog.FileName;

                // استخراج قائمة الملفات داخل الأرشيف
                var filesInArchive = HuffmanCompressor.ListFilesInArchive(archivePath);
                if (filesInArchive == null || filesInArchive.Count == 0)
                {
                    System.Windows.MessageBox.Show("الأرشيف فارغ أو تالف.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // اختيار الملف من قائمة
                var selectFileDialog = new SelectFileDialog(filesInArchive); 
                if (selectFileDialog.ShowDialog() == true)
                {
                    string selectedFile = selectFileDialog.SelectedFile;

                    var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
                    if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string outputFolder = folderDialog.SelectedPath;

                        try
                        {
                            HuffmanCompressor.ExtractSingleFile(archivePath, selectedFile, outputFolder);

                            ExtractionResultsListBox.Items.Add($"✅ تم استخراج: {selectedFile}");
                            
                            System.Windows.MessageBox.Show($"تم استخراج {selectedFile} بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"حدث خطأ أثناء استخراج الملف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }


        ////=============================================================>
        public void UpdateFileCount() => FileCountText.Text = $"عدد الملفات: {FilesListBox.Items.Count}";

        ////=============================================================>

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            var files = FileService.SelectFiles();
            foreach (var file in files)
            {
                if (!FilesListBox.Items.Contains(file))
                    FilesListBox.Items.Add(file);
            }
            UpdateFileCount();
        }

        ////=============================================================>

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            var files = FolderService.SelectFolderFiles();
            foreach (var file in files)
            {
                if (!FilesListBox.Items.Contains(file))
                    FilesListBox.Items.Add(file);
            }
            UpdateFileCount();
        }

        ////=============================================================>

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = FilesListBox.SelectedItems;
            var itemsToRemove = new System.Collections.Generic.List<object>();

            foreach (var item in selectedItems)
                itemsToRemove.Add(item);

            foreach (var item in itemsToRemove)
                FilesListBox.Items.Remove(item);

            UpdateFileCount();
        }

        ////=============================================================>

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }

        ////=============================================================>

        private async void StartCompression_Click(object sender, RoutedEventArgs e)
        {
            var algorithm = (AlgorithmComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

            if (string.IsNullOrEmpty(algorithm))
            {
                System.Windows.MessageBox.Show("يرجى اختيار خوارزمية الضغط أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (FilesListBox.Items.Count == 0)
            {
                System.Windows.MessageBox.Show("يرجى إضافة ملفات أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CompressionResultsListBox.Items.Clear();
            ProgressBar.IsIndeterminate = true;

            _cts = new CancellationTokenSource();
            CancelButton.Visibility = Visibility.Visible;

            var fileList = new List<string>();
            foreach (var item in FilesListBox.Items)
                fileList.Add(item.ToString());

            string archivePath = "archive.huf";

            try
            {
                var results = await Task.Run(() =>
                    CompressionService.CompressToArchive(fileList, algorithm, archivePath, _cts.Token)
                );

                if (_cts.Token.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

                var result = results;

                if (!string.IsNullOrEmpty(result.Error))
                {
                    CompressionResultsListBox.Items.Add($"❌ {result.FileName} - خطأ: {result.Error}");
                }
                else
                {
                    CompressionResultsListBox.Items.Add(
                        $"📄 {result.FileName}\n" +
                        $"  ⮕ الحجم الأصلي: {result.OriginalSize} بايت\n" +
                        $"  ⮕ الحجم بعد الضغط: {result.CompressedSize} بايت\n" +
                        $"  ⮕ نسبة الضغط: {result.CompressionRatio * 100:F2}%"
                    );
                }

                System.Windows.MessageBox.Show("تم ضغط الملفات بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                System.Windows.MessageBox.Show("تم إلغاء عملية الضغط.", "تم الإلغاء", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"حدث خطأ أثناء الضغط:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ProgressBar.IsIndeterminate = false;
                CancelButton.Visibility = Visibility.Collapsed;
                _cts.Dispose();
                _cts = null;
            }
        }
    }
}