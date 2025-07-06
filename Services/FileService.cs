using Microsoft.Win32;
using WinForms = Microsoft.Win32;
using System.Collections.Generic;
using System.IO;

namespace FileCompressorApp.Services
{
    public static class FileService
    {
        public static List<string> SelectFiles()
        {
            var openFileDialog = new WinForms.OpenFileDialog
            {
                Title = "اختر الملفات",
                Multiselect = true,
                Filter = "كل الملفات|*.*"
            };

            var selectedFiles = new List<string>();

            if (openFileDialog.ShowDialog() == true)
            {
                selectedFiles.AddRange(openFileDialog.FileNames);
            }

            return selectedFiles;
        }

        public static string SelectArchiveFile()
        {
            var dialog = new  Microsoft.Win32.OpenFileDialog
            {
                Filter = "Compressed Archive (*.bin)|*.bin|All Files (*.*)|*.*"
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public static string SelectOutputDirectory()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
        }


    }
}
