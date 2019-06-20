using Microsoft.Win32;
using System;
using System.IO;

namespace arma_launcher.Properties
{


    // This class allows you to handle specific events on the settings class:
    //  The SettingChanging event is raised before a setting's value is changed.
    //  The PropertyChanged event is raised after a setting's value is changed.
    //  The SettingsLoaded event is raised after the setting values are loaded.
    //  The SettingsSaving event is raised before the setting values are saved.
    internal sealed partial class Settings
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private Settings()
        {
            LocalFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), LocalFolder);

            try
            {
                var armaRegKey = Registry.LocalMachine.OpenSubKey(@"Software\WOW6432Node\bohemia interactive\arma 3");

                if (armaRegKey == null) return;

                var armaPath = armaRegKey.GetValue("main").ToString();
                armaRegKey.Close();

                Properties["A3Path"].DefaultValue = armaPath;
                Properties["A3ModsPath"].DefaultValue = armaPath;

                A3Path = armaPath;
                A3ModsPath = armaPath;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Settings Error");
            }
        }
    }
}
