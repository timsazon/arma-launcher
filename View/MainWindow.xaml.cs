using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using arma_launcher.ModService;
using arma_launcher.ModService.Impl;
using arma_launcher.Properties;
using MaterialDesignThemes.Wpf;
using NLog;

namespace arma_launcher
{
    public partial class MainWindow : Window
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IModService _modService;

        private CancellationTokenSource _cancellationTokenSource;

        private (
            IEnumerable<Addon> validFiles,
            IEnumerable<Addon> updateFiles,
            IEnumerable<Addon> newFiles,
            IEnumerable<Addon> deleteFiles) _validationInfo;

        public MainWindow()
        {
            InitializeComponent();
            _modService = new FtpModService();
        }

        private Progress<ProgressMessage> ShowProgress()
        {
            PlayButton.IsEnabled = false;
            SettingsButton.IsEnabled = false;

            ValidateFull.Visibility = Visibility.Hidden;
            ValidateButton.Visibility = Visibility.Hidden;

            ProgressBarTotal.Margin = new Thickness(20, 0, 80, 40);
            ProgressBarTotal.Height = 80;
            ProgressBarTotal.Visibility = Visibility.Visible;

            ProgressBarMessage.Text = "";
            ProgressBarMessage.Visibility = Visibility.Visible;

            return new Progress<ProgressMessage>(v =>
            {
                if (v.TotalProgress < 0)
                {
                    if (v.ProgressStatus != ProgressMessage.Status.Downloading)
                    {
                        TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
                        ProgressBarTotal.IsIndeterminate = true;
                        ProgressBar.Visibility = Visibility.Hidden;
                    }
                }
                else
                {
                    if (v.ProgressStatus == ProgressMessage.Status.Downloading)
                    {
                        TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                        TaskbarItemInfo.ProgressValue = v.TotalProgress / 100.0;
                        ProgressBarTotal.Margin = new Thickness(20, 0, 80, 50);
                        ProgressBarTotal.IsIndeterminate = false;
                        ProgressBarTotal.Height = 70;
                        ProgressBarTotal.Value = v.TotalProgress;
                        ProgressBar.Visibility = Visibility.Visible;
                        ProgressBar.Value = v.Progress;
                    }
                    else
                    {
                        TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                        TaskbarItemInfo.ProgressValue = v.TotalProgress / 100.0;
                        ProgressBarTotal.IsIndeterminate = false;
                        ProgressBarTotal.Value = v.TotalProgress;
                        ProgressBar.Visibility = Visibility.Hidden;
                    }
                }

                switch (v.ProgressStatus)
                {
                    case ProgressMessage.Status.Connecting:
                        ProgressBarMessage.Text = Properties.Resources.Connecting;
                        break;
                    case ProgressMessage.Status.Validating:
                        ProgressBarMessage.Text = Properties.Resources.Validating;
                        break;
                    case ProgressMessage.Status.Downloading:
                        ProgressBarMessage.Text =
                            $"{Properties.Resources.Downloading} {v.FileName} ({v.TransferSpeedToString()}), {Properties.Resources.ETA} ~{v.ETA:hh\\:mm\\:ss}";
                        break;
                    default:
                        ProgressBarMessage.Text = "";
                        break;
                }
            });
        }

        private void HideProgress()
        {
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            TaskbarItemInfo.ProgressValue = 0.0;

            ProgressBarTotal.Visibility = Visibility.Hidden;
            ProgressBar.Visibility = Visibility.Hidden;
            ProgressBarMessage.Visibility = Visibility.Hidden;

            DownloadCancelButton.Visibility = Visibility.Hidden;

            ValidateFull.Visibility = Visibility.Visible;
            ValidateButton.Visibility = Visibility.Visible;

            PlayButton.IsEnabled = true;
            SettingsButton.IsEnabled = true;
        }

        private async void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var full = ValidateFull.IsChecked ?? false;
                var progress = ShowProgress();
                _validationInfo = await Task.Run(() => _modService.Validate(full, progress));
                HideProgress();

                if (!_validationInfo.newFiles.Any() &&
                    !_validationInfo.updateFiles.Any() &&
                    !_validationInfo.deleteFiles.Any())
                {
                    Snackbar.MessageQueue.Enqueue(Properties.Resources.NoUpdateRequired);
                    return;
                }

                await DialogHost.Show(new DownloadDialog(_validationInfo), DownloadDialog_Close);
            }
            catch (Exception ex)
            {
                Snackbar.MessageQueue.Enqueue($"{Properties.Resources.Error}: {ex.Message}");
                Logger.Error(ex, "Validate");
                HideProgress();
            }
        }

        private async void DownloadDialog_Close(object sender, DialogClosingEventArgs e)
        {
            if (!(bool) e.Parameter) return;

            try
            {
                foreach (var file in _validationInfo.deleteFiles)
                    File.Delete(Path.Combine(Settings.Default.A3ModsPath, file.Path, file.Pbo));

                DownloadCancelButton.Visibility = Visibility.Visible;
                _cancellationTokenSource = new CancellationTokenSource();
                var progress = ShowProgress();
                await Task.Run(() => _modService.Download(_validationInfo.updateFiles.Concat(_validationInfo.newFiles),
                    progress,
                    _cancellationTokenSource.Token));

                Snackbar.MessageQueue.Enqueue(Properties.Resources.UpdateInstalled);
            }
            catch (Exception ex)
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    Snackbar.MessageQueue.Enqueue($"{Properties.Resources.Error}: {ex.Message}");
                    Logger.Error(ex, "Download");
                }
            }
            finally
            {
                HideProgress();
            }
        }

        private void DownloadCancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var workshopMods = Settings.Default.A3WorkshopMods.Split(';');
                var mods = Settings.Default.A3Mods.Split(';').Select(mod =>
                        workshopMods.Contains(mod)
                            ? Path.Combine(Settings.Default.A3Path, "!Workshop", mod)
                            : Path.Combine(Settings.Default.A3ModsPath, mod))
                    .ToList();

                var a3Exe = Path.Combine(Settings.Default.A3Path, "arma3battleye.exe");

                var args = "";

                if (Settings.Default.WindowFlag) args += " -window";
                if (Settings.Default.NoSplashFlag) args += " -noSplash";
                if (Settings.Default.SkipIntroFlag) args += " -skipIntro";
                if (Settings.Default.NoLogsFlag) args += " -noLogs";
                if (Settings.Default.EnableHTFlag) args += " -enableHT";
                if (Settings.Default.HugePagesFlag) args += " -hugepages";

                if (mods.Any()) args += $" -mod=\"{string.Join(";", mods)}\"";

                Process.Start(a3Exe, args);

                WindowState = WindowState.Minimized;
            }
            catch (Exception ex)
            {
                Snackbar.MessageQueue.Enqueue($"{Properties.Resources.Error}: {ex.Message}");
                Logger.Error(ex, "Launch");
            }
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            await DialogHost.Show(new SettingsDialog(),
                delegate(object _, DialogClosingEventArgs args) { Settings.Default.Save(); });
        }

        private async void TeamSpeakButton_Click(object sender, RoutedEventArgs e)
        {
            TeamSpeakButton.IsHitTestVisible = false;
            TeamSpeakButton.SetCurrentValue(ButtonProgressAssist.IsIndicatorVisibleProperty, true);
            TeamSpeakButton.SetCurrentValue(ButtonProgressAssist.IsIndeterminateProperty, true);
            TeamSpeakButton.SetCurrentValue(ButtonProgressAssist.ValueProperty, -1.0);

            try
            {
                await TeamSpeakHelper.CheckAndInstall(TeamSpeakButton);
            }
            catch (Exception ex)
            {
                Snackbar.MessageQueue.Enqueue($"{Properties.Resources.Error}: {ex.Message}");
                Logger.Error(ex, "TeamSpeak");
            }

            TeamSpeakButton.SetCurrentValue(ButtonProgressAssist.IsIndicatorVisibleProperty, false);
            TeamSpeakButton.IsHitTestVisible = true;
        }
    }
}