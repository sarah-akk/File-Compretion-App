using WinForms = System.Windows.Forms;
using System.Collections.Generic;
using System.IO;

namespace FileCompressorApp.Services
{
    public static class FolderService
    {
        public static List<string> SelectFolderFiles()
        {
            var files = new List<string>();

            using (var folderDialog = new WinForms.FolderBrowserDialog())
            {
                folderDialog.Description = "اختر مجلدًا لإضافة جميع ملفاته";
                folderDialog.ShowNewFolderButton = false;

                if (folderDialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    var folderPath = folderDialog.SelectedPath;
                    files.AddRange(Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories));
                    return new List<string>(files);
                }
            }

            return new List<string>();
        }
    }
}
