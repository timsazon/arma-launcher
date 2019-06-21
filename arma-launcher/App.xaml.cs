using System.Globalization;
using System.Net;
using System.Threading;
using arma_launcher.Properties;

namespace arma_launcher
{
    public partial class App
    {
        private App()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var culture = new CultureInfo(Settings.Default.Language);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
    }
}