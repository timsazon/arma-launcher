using System.Windows.Controls;

namespace arma_launcher
{
    public partial class WarningDialog : UserControl
    {
        public WarningDialog(string message)
        {
            InitializeComponent();

            TextContent.Text = message;
        }
    }
}