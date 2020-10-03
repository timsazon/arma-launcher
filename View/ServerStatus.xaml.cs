using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using arma_launcher.Properties;
using Newtonsoft.Json;
using NLog;

namespace arma_launcher.View
{
    public class ServerInfo
    {
        public CData Data { get; set; }

        public class CData
        {
            public CAttributes Attributes { get; set; }

            public class CAttributes
            {
                public string Name { get; set; }
                public string Ip { get; set; }
                public string Port { get; set; }
                public int Players { get; set; }
                public int MaxPlayers { get; set; }
                public string Status { get; set; }
            }
        }
    }

    public partial class ServerStatus
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static readonly DependencyProperty IdProperty =
            DependencyProperty.Register(
                "Id",
                typeof(string),
                typeof(ServerStatus),
                new PropertyMetadata(default(string), IdPropertyChanged));

        public ServerStatus()
        {
            InitializeComponent();
        }

        private Timer UpdateTimer { get; set; }

        public string Id
        {
            get => (string) GetValue(IdProperty);
            set => SetValue(IdProperty, value);
        }

        private void UpdateChip(ServerInfo info)
        {
            if (info == null)
            {
                Chip.Visibility = Visibility.Hidden;
                return;
            }

            if (info.Data.Attributes.Status == "online")
            {
                Chip.Content =
                    $"{info.Data.Attributes.Name} {info.Data.Attributes.Players}/{info.Data.Attributes.MaxPlayers}";
                Chip.IconBackground = Brushes.Green;
            }
            else
            {
                Chip.Content = $"{info.Data.Attributes.Name}";
                Chip.IconBackground = Brushes.DarkRed;
            }

            Chip.Visibility = Visibility.Visible;
        }

        private static void IdPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var serverStatus = (ServerStatus) obj;
            if (string.IsNullOrEmpty(serverStatus.Id)) return;

            serverStatus.Dispatcher.Invoke(() =>
                LoadInfo("https://api.battlemetrics.com/servers/" + serverStatus.Id, serverStatus.UpdateChip));

            var timer = new Timer {Interval = Settings.Default.BMRefreshTime, Enabled = true};
            timer.Elapsed += delegate
            {
                serverStatus.Dispatcher.Invoke(() =>
                    LoadInfo("https://api.battlemetrics.com/servers/" + serverStatus.Id, serverStatus.UpdateChip));
            };

            serverStatus.UpdateTimer?.Dispose();
            serverStatus.UpdateTimer = timer;
        }

        private static async void LoadInfo(string uri, Action<ServerInfo> infoCallback)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var requestMessage = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(uri)
                    };
                    requestMessage.Headers.Authorization =
                        AuthenticationHeaderValue.Parse("Bearer " + Settings.Default.BMToken);
                    var response = await client.SendAsync(requestMessage);
                    var content = await response.Content.ReadAsStringAsync();
                    var info = JsonConvert.DeserializeObject<ServerInfo>(content);

                    infoCallback(info);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Server Status");
                }
            }
        }
    }
}