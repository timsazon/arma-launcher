using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using Squirrel;
using Settings = arma_launcher.Properties.Settings;

namespace arma_launcher
{
    public partial class App
    {
        private App()
        {
            var culture = new CultureInfo(Settings.Default.Language);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }

        private async void App_Startup(object sender, StartupEventArgs e)
        {
            if (Settings.Default.UpdateUrl.Length <= 0) return;
            using (var mgr = new UpdateManager(Settings.Default.UpdateUrl))
            {
                await mgr.UpdateApp();
            }
        }
    }
}
