using System.Collections.Generic;
using System.Windows;

namespace FileCompressorApp
{
    public partial class SelectFileDialog : Window
    {
        public string SelectedFile { get; private set; }

        public SelectFileDialog(List<string> files)
        {
            InitializeComponent();
            FilesListBox.ItemsSource = files;
            if (files.Count > 0)
                FilesListBox.SelectedIndex = 0;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilesListBox.SelectedItem != null)
            {
                SelectedFile = FilesListBox.SelectedItem.ToString();
                DialogResult = true;
            }
            else
            {
                System.Windows.MessageBox.Show("يرجى اختيار ملف.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
