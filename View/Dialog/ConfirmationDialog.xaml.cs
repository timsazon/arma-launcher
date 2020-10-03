namespace arma_launcher.View.Dialog
{
    public partial class ConfirmationDialog
    {
        public ConfirmationDialog(string message)
        {
            InitializeComponent();

            TextContent.Text = message;
        }
    }
}