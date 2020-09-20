using System.Windows.Controls;

namespace arma_launcher
{
    public partial class ConfirmationDialog : UserControl
    {
        public ConfirmationDialog(string message)
        {
            InitializeComponent();

            TextContent.Text = message;
        }
    }
}