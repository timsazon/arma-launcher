using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Controls;
using arma_launcher.Properties;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace arma_launcher
{
    public static class TeamSpeakHelper
    {
        public static async Task CheckAndInstall(Button button)
        {
            var teamSpeakDirectory = await GetTeamSpeakDirectory();
            if (teamSpeakDirectory == null)
            {
                var teamSpeakInstaller = Path.Combine(Settings.Default.LocalFolder, "TeamSpeak3_Installer.exe");
                var isInstallerExist = File.Exists(teamSpeakInstaller);

                var accept = (bool)await DialogHost.Show(new ConfirmationDialog(
                    isInstallerExist
                        ? Resources.InstallTS
                        : $"{Resources.InstallTS} {Resources.InstallerDownloading}"));
                if (!accept) return;

                if (!isInstallerExist)
                    using (var client = new WebClient())
                    {
                        button.SetCurrentValue(ButtonProgressAssist.IsIndeterminateProperty, false);
                        client.DownloadProgressChanged += delegate (object _, DownloadProgressChangedEventArgs args)
                        {
                            button.SetCurrentValue(ButtonProgressAssist.ValueProperty,
                                (double)args.ProgressPercentage);
                        };
                        await client.DownloadFileTaskAsync(Settings.Default.TeamSpeakUrl, teamSpeakInstaller + ".temp");
                        File.Move(teamSpeakInstaller + ".temp", teamSpeakInstaller);
                        button.SetCurrentValue(ButtonProgressAssist.IsIndeterminateProperty, true);
                        button.SetCurrentValue(ButtonProgressAssist.ValueProperty, -1.0);
                    }

                var proc = Process.Start(teamSpeakInstaller);
                await Task.Run(() => proc?.WaitForExit());
                teamSpeakDirectory = await GetTeamSpeakDirectory();
                if (teamSpeakDirectory == null) return;
            }

            var isPluginInstalled = await IsPluginInstalled(teamSpeakDirectory);
            if (!isPluginInstalled)
            {
                var accept = (bool)await DialogHost.Show(new ConfirmationDialog(Resources.InstallPlugin));
                if (!accept) return;

                var pluginInstaller = Path.Combine(Settings.Default.LocalFolder, "task_force_radio.ts3_plugin");

                if (!File.Exists(pluginInstaller))
                    using (var client = new WebClient())
                    {
                        button.SetCurrentValue(ButtonProgressAssist.IsIndeterminateProperty, false);
                        client.DownloadProgressChanged += delegate (object _, DownloadProgressChangedEventArgs args)
                        {
                            button.SetCurrentValue(ButtonProgressAssist.ValueProperty,
                                (double)args.ProgressPercentage);
                        };
                        await client.DownloadFileTaskAsync(Settings.Default.TaskForcePluginUrl,
                            pluginInstaller + ".temp");
                        File.Move(pluginInstaller + ".temp", pluginInstaller);
                        button.SetCurrentValue(ButtonProgressAssist.IsIndeterminateProperty, true);
                        button.SetCurrentValue(ButtonProgressAssist.ValueProperty, -1.0);
                    }

                var proc = Process.Start(pluginInstaller);
                await Task.Run(() => proc?.WaitForExit());
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = Settings.Default.TeamSpeakServer,
                UseShellExecute = true
            });
        }

        private static async Task<string> GetTeamSpeakDirectory()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var tsRegKey = Registry.LocalMachine.OpenSubKey(@"Software\TeamSpeak 3 Client");

                    if (tsRegKey?.GetValue("") == null)
                    {
                        tsRegKey = Registry.CurrentUser.OpenSubKey(@"Software\TeamSpeak 3 Client");
                        if (tsRegKey?.GetValue("") == null) return null;
                    }

                    var teamSpeakPath = tsRegKey.GetValue("").ToString();
                    tsRegKey.Close();

                    return teamSpeakPath;
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }

        private static async Task<bool> IsPluginInstalled(string teamSpeakDirectory)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var exist = File.Exists(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TS3Client",
                            "plugins", "task_force_radio_win64.dll")
                    );
                    if (exist) return true;

                    exist = File.Exists(Path.Combine(teamSpeakDirectory, "config", "plugins",
                        "task_force_radio_win64.dll"));
                    if (exist) return true;

                    exist = File.Exists(Path.Combine(teamSpeakDirectory, "plugins", "task_force_radio_win64.dll"));
                    return exist;
                }
                catch (Exception)
                {
                    return false;
                }
            });
        }
    }
}