using File_Compretion_App.Views;
using FileCompressorApp.Services;
using System.IO;
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

        private void UsePasswordCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            PasswordBox.Visibility = Visibility.Visible;
        }

        private void UsePasswordCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            PasswordBox.Visibility = Visibility.Collapsed;
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

                        string? userPassword = null;

                        // هنا فقط نفتح الملف لقراءة كلمة السر (واحد فقط)، ثم نغلقه فوراً
                        using (var archiveStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read))
                        using (var reader = new BinaryReader(archiveStream))
                        {
                            int fileCount = reader.ReadInt32();
                            if (fileCount == 0)
                                throw new Exception("الأرشيف فارغ.");

                            int fileNameLen = reader.ReadInt32();
                            reader.ReadChars(fileNameLen);

                            int passwordLen = reader.ReadInt32();
                            string? archivePassword = passwordLen > 0 ? new string(reader.ReadChars(passwordLen)) : null;

                            if (!string.IsNullOrEmpty(archivePassword))
                            {
                                // إظهار نافذة طلب كلمة السر في الثريد الرئيسي
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    var passwordPrompt = new PasswordPromptDialog();
                                    if (passwordPrompt.ShowDialog() == true)
                                    {
                                        userPassword = passwordPrompt.PasswordBox.Password;
                                    }
                                    else
                                    {
                                        throw new OperationCanceledException("تم إلغاء فك الضغط بسبب عدم إدخال كلمة السر.");
                                    }
                                });
                            }
                        }

                        var progress = new Progress<int>(percent =>
                        {
                            DecompressionProgressBar.Value = percent;
                        });

                        // الآن نستدعي فك الضغط والذي يفتح الملف بنفسه
                        await Task.Run(() =>
                        {
                            CompressionService.DecompressArchive(archivePath, outputFolder, _cts.Token, progress, userPassword);
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

                string? userPassword = null;
                string? archivePassword = null;

                try
                {
                    // قراءة كلمة السر من الأرشيف
                    using (var archiveStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read))
                    using (var reader = new BinaryReader(archiveStream))
                    {
                        int fileCount = reader.ReadInt32();
                        if (fileCount == 0)
                            throw new Exception("الأرشيف فارغ.");

                        int fileNameLen = reader.ReadInt32();
                        reader.ReadChars(fileNameLen);

                        int passwordLen = reader.ReadInt32();
                        archivePassword = passwordLen > 0 ? new string(reader.ReadChars(passwordLen)) : null;

                        if (!string.IsNullOrEmpty(archivePassword))
                        {
                            var passwordPrompt = new PasswordPromptDialog();
                            if (passwordPrompt.ShowDialog() == true)
                            {
                                userPassword = passwordPrompt.PasswordBox.Password;

                                // لغايات الـ Debug فقط
                                System.Windows.MessageBox.Show(
                                    $"🔐 كلمة السر المخزنة: {archivePassword}\n🔑 كلمة السر المدخلة: {userPassword}",
                                    "Debug Password Check",
                                    MessageBoxButton.OK, MessageBoxImage.Information
                                );

                                if (archivePassword != userPassword)
                                {
                                    System.Windows.MessageBox.Show("❌ كلمة السر غير صحيحة.", "خطأ في كلمة السر", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }
                            }
                            else
                            {
                                return; // المستخدم ألغى
                            }
                        }
                    }

                    // استخراج قائمة الملفات داخل الأرشيف
                    var filesInArchive = HuffmanCompressor.ListFilesInArchive(archivePath);
                    if (filesInArchive == null || filesInArchive.Count == 0)
                    {
                        System.Windows.MessageBox.Show("الأرشيف فارغ أو تالف.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // اختيار الملف من القائمة
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
                                CompressionService.ExtractSingleFile(archivePath, selectedFile, outputFolder, userPassword);

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
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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

            string? password = UsePasswordCheckBox.IsChecked == true
            ? PasswordBox.Password
            : null;

            try
            {
                var results = await Task.Run(() =>
                    CompressionService.CompressToArchive(fileList, algorithm, archivePath, _cts.Token , password)
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