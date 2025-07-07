using System.Windows;
using System.Windows.Controls;


namespace File_Compretion_App.Views

{
    public partial class PasswordPromptDialog : Window
    {
        public string EnteredPassword => PasswordBox.Password;

        public PasswordPromptDialog()
        {
            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
