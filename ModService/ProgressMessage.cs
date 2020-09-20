using System;

namespace arma_launcher.ModService
{
    public class ProgressMessage
    {
        public enum Status
        {
            Connecting,
            Validating,
            Checksum,
            Downloading,
            Done,
            Error,
            None
        }

        public Status ProgressStatus { get; set; }

        public double TotalProgress { get; set; }

        public double Progress { get; set; }

        public double TransferSpeed { get; set; }

        public TimeSpan ETA { get; set; }

        public string FileName { get; set; }

        public ProgressMessage(Status status, double totalProgress = -1, double progress = -1, double transferSpeed = 0, TimeSpan remainingTime = default, string fileName = default)
        {
            ProgressStatus = status;
            TotalProgress = totalProgress;
            Progress = progress;
            TransferSpeed = transferSpeed;
            ETA = remainingTime;
            FileName = fileName;
        }

        public string TransferSpeedToString()
        {
            double value = TransferSpeed / 1024;

            if (value < 1024)
            {
                return $"{Math.Round(value)} KB/s";
            }
            else
            {
                value = value / 1024;
                return $"{Math.Round(value, 1)} MB/s";
            }
        }
    }
}
