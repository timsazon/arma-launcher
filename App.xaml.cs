using System.Windows;
using System.Globalization;
using System.Net;
using System.Threading;
using arma_launcher.Properties;
using NLog;

namespace arma_launcher
{
    public partial class App : Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private App()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var culture = new CultureInfo(Settings.Default.Language);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            Dispatcher.UnhandledException += OnDispatcherUnhandledException;
        }

        private static void OnDispatcherUnhandledException(object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Error(e.Exception, e.Exception.Message);
            MessageBox.Show("Unhandled exception occurred: \n" + e.Exception.Message, "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}