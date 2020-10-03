using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using arma_launcher.ModService;

namespace arma_launcher.View.Dialog
{
    public partial class DownloadDialog
    {
        public DownloadDialog(
            (IEnumerable<Addon> validFiles, IEnumerable<Addon> updateFiles, IEnumerable<Addon> newFiles,
                IEnumerable<Addon> deleteFiles) validationInfo)
        {
            InitializeComponent();

            BuildDialog(validationInfo);
        }

        private void BuildDialog(
            (IEnumerable<Addon> validFiles, IEnumerable<Addon> updateFiles, IEnumerable<Addon> newFiles,
                IEnumerable<Addon> deleteFiles) validationInfo)
        {
            var newFilesTree = new TreeViewItem();
            long newFilesSize = 0;
            var (_, updateFiles, newFiles, deleteFiles) = validationInfo;
            foreach (var file in newFiles)
            {
                var item = new TreeViewItem
                {
                    Header = $"{Path.Combine(file.Path, file.Pbo)} ({BytesToString(file.Size)})"
                };
                newFilesSize += file.Size;
                newFilesTree.Items.Add(item);
            }

            if (newFilesTree.Items.Count > 0)
            {
                newFilesTree.Header = $"{Properties.Resources.NewFiles} ({BytesToString(newFilesSize)})";
                DownloadTreeView.Items.Add(newFilesTree);
            }

            var updateFilesTree = new TreeViewItem();
            long updateFilesSize = 0;
            foreach (var file in updateFiles)
            {
                var item = new TreeViewItem
                {
                    Header = $"{Path.Combine(file.Path, file.Pbo)} ({BytesToString(file.Size)})"
                };
                updateFilesSize += file.Size;
                updateFilesTree.Items.Add(item);
            }

            if (updateFilesTree.Items.Count > 0)
            {
                updateFilesTree.Header = $"{Properties.Resources.UpdateFiles} ({BytesToString(updateFilesSize)})";
                DownloadTreeView.Items.Add(updateFilesTree);
            }

            var deleteFilesTree = new TreeViewItem
            {
                Header = $"{Properties.Resources.DeleteFiles}"
            };
            foreach (var file in deleteFiles)
            {
                var item = new TreeViewItem
                {
                    Header = Path.Combine(file.Path, file.Pbo)
                };
                deleteFilesTree.Items.Add(item);
            }

            if (deleteFilesTree.Items.Count > 0) DownloadTreeView.Items.Add(deleteFilesTree);
        }

        private static string BytesToString(long byteCount)
        {
            string[] suf = {"B", "KB", "MB", "GB"};
            if (byteCount == 0)
                return "0" + suf[0];
            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return Math.Sign(byteCount) * num + suf[place];
        }
    }
}