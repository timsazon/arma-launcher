using System.Windows;
using System.Windows.Controls;
using arma_launcher.Properties;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace arma_launcher
{
    public partial class SettingsDialog : UserControl
    {
        public SettingsDialog()
        {
            InitializeComponent();
        }

        private void A3PathButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog {IsFolderPicker = true})
            {
                if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;

                Settings.Default.A3Path = dialog.FileName;
            }
        }

        private void A3ModsPathButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog {IsFolderPicker = true})
            {
                if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;

                Settings.Default.A3ModsPath = dialog.FileName;
            }
        }
    }
}