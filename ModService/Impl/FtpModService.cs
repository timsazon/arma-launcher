﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using arma_launcher.Properties;
using FluentFTP;
using Newtonsoft.Json;
using NLog;
using Polly;
using Polly.CircuitBreaker;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;

namespace arma_launcher.ModService.Impl
{
    internal class FtpModService : IModService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly MD5 Md5 = MD5.Create();

        private Uri _ftpUri;
        private NetworkCredential _networkCredential;
        private FtpClient _client;

        public FtpModService()
        {
            FtpTrace.LogUserName = false;
            FtpTrace.LogPassword = false;
            FtpTrace.LogIP = false;
            FtpTrace.LogFunctions = false;
        }

        public async Task<(IEnumerable<Addon>, IEnumerable<Addon>, IEnumerable<Addon>, IEnumerable<Addon>)> Validate(
            bool full, IProgress<ProgressMessage> progress)
        {
            UpdateConnectionInfo();

            using (_client = new FtpClient(_ftpUri.Host, _networkCredential))
            {
                progress.Report(new ProgressMessage(ProgressMessage.Status.Connecting));

                await FetchAutoconfig();

                progress.Report(new ProgressMessage(ProgressMessage.Status.Validating));

                var modsTask = GetMods();
                var addonsTask = GetAddons();
                var oldValidTask = GetValid();

                var mods = await modsTask;
                var addons = await addonsTask;
                var oldValid = await oldValidTask;

                var validFiles = new ConcurrentBag<Addon>();
                var updateFiles = new ConcurrentBag<Addon>();
                var deleteFiles = new ConcurrentBag<Addon>();

                var validationProgress = 0;

                void ValidationProgressIncrement()
                {
                    progress.Report(new ProgressMessage(ProgressMessage.Status.Validating,
                        (double) Interlocked.Increment(ref validationProgress) / addons.Count * 100.0));
                }

                mods.ForEach(mod =>
                    ValidateMod(
                        mod, addons, ValidationProgressIncrement,
                        validFiles, updateFiles, deleteFiles,
                        full, oldValid)
                );

                GC.Collect(); // Free memory from allocated hash's byte arrays

                var newFiles = addons.FindAll(a =>
                    !(validFiles.Contains(a) || updateFiles.Contains(a) || deleteFiles.Contains(a)));

                await SaveValid(validFiles);

                progress.Report(new ProgressMessage(ProgressMessage.Status.Done));

                return (validFiles, updateFiles, newFiles, deleteFiles);
            }
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public async Task Download(IEnumerable<Addon> downloadFiles, IProgress<ProgressMessage> progress,
            CancellationToken cancellation)
        {
            var circuitBreakerPolicy = Policy.Handle<Exception>().AdvancedCircuitBreakerAsync(
                1,
                TimeSpan.FromSeconds(10),
                3,
                TimeSpan.MaxValue
            );

            var retryPolicy = Policy.Handle<Exception>(e => !(e is BrokenCircuitException)).WaitAndRetryForeverAsync(
                retryAttempt => TimeSpan.FromSeconds(2),
                (exception, _) => Logger.Error(exception, "Download")
            );

            using (_client = new FtpClient(_ftpUri.Host, _networkCredential))
            {
                var totalProgress = 0.0;
                var totalSize = downloadFiles.Sum(a => a.Size);

                var unpackTasks = new List<Task>();

                foreach (var addon in downloadFiles)
                {
                    var path = Path.Combine(Settings.Default.A3ModsPath, addon.Url);
                    var remotePath =
                        Path.Combine(
                            _ftpUri.AbsolutePath.Remove(_ftpUri.AbsolutePath.Length - _ftpUri.Segments.Last().Length),
                            addon.Url);

                    if (File.Exists(path))
                    {
                        File.SetAttributes(path, FileAttributes.Normal);
                        File.Delete(path);
                    }

                    var total = totalProgress;
                    var ftpProgress = new Progress<FtpProgress>(v =>
                    {
                        var downloaded = addon.Size / 100.0 * v.Progress;
                        progress.Report(
                            new ProgressMessage(
                                ProgressMessage.Status.Downloading, total + downloaded / totalSize * 100.0, v.Progress,
                                v.TransferSpeed, v.ETA,
                                addon.Pbo));
                    });

                    await retryPolicy
                        .WrapAsync(circuitBreakerPolicy)
                        .ExecuteAsync(async token =>
                        {
                            await _client.DownloadFileAsync(
                                path,
                                remotePath,
                                FtpLocalExists.Append,
                                FtpVerify.None,
                                ftpProgress,
                                token
                            );
                        }, cancellation);

                    totalProgress += (double) addon.Size / totalSize * 100.0;

                    unpackTasks.Add(Task.Run(() => UnpackFile(path), cancellation));
                }

                await Task.WhenAll(unpackTasks);
            }
        }

        private async Task FetchAutoconfig()
        {
            await _client.SetWorkingDirectoryAsync(
                _ftpUri.AbsolutePath.Remove(_ftpUri.AbsolutePath.Length - _ftpUri.Segments.Last().Length));
            await _client.DownloadFileAsync(
                Path.Combine(Settings.Default.LocalFolder, "md5", _ftpUri.Segments.Last()),
                _ftpUri.Segments.Last(), FtpLocalExists.Overwrite
            );

            await Task.Run(() =>
                UnpackFile(Path.Combine(Settings.Default.LocalFolder, "md5", _ftpUri.Segments.Last())));
        }

        private void UpdateConnectionInfo()
        {
            _ftpUri = new Uri(Settings.Default.FtpUri);

            var userName = string.Empty;
            var password = string.Empty;
            var items = _ftpUri.UserInfo.Split(':');
            if (items.Length > 0) userName = items[0];
            if (items.Length > 1) password = items[1];

            _networkCredential = new NetworkCredential(userName, password);
        }

        private static void ValidateMod(string mod, List<Addon> addons, Action progress,
            ConcurrentBag<Addon> validFiles,
            ConcurrentBag<Addon> updateFiles, ConcurrentBag<Addon> deleteFiles, bool full = false,
            List<Addon> oldValid = default)
        {
            var modPath = Path.Combine(Settings.Default.A3ModsPath, mod);
            var modFiles = Directory.Exists(modPath)
                ? Directory.EnumerateFiles(modPath, "*.*", SearchOption.AllDirectories)
                : new List<string>();

            foreach (var file in modFiles)
            {
                var localFile = file.Substring(
                    Settings.Default.A3ModsPath.Length + 1,
                    file.Length - Settings.Default.A3ModsPath.Length - 1
                );

                var localFileInfo = new FileInfo(file);

                var localAddon = new Addon
                {
                    Path = Path.GetDirectoryName(localFile),
                    Pbo = Path.GetFileName(localFile),
                    Size = localFileInfo.Length,
                    Time = localFileInfo.LastWriteTime.Millisecond
                };

                var remoteAddon = addons.Find(a => a.Equals(localAddon));

                if (Equals(remoteAddon, default(Addon)))
                {
                    deleteFiles.Add(localAddon);
                    progress();
                    continue;
                }

                if (!full && oldValid != null)
                {
                    var exist = oldValid.Exists(a =>
                        a.Path == localAddon.Path && a.Pbo == localAddon.Pbo && a.Time == localAddon.Time);
                    localAddon.Md5 = exist ? remoteAddon.Md5 : CalculateMd5(file, localFileInfo.Length);
                }
                else
                {
                    localAddon.Md5 = CalculateMd5(file, localFileInfo.Length);
                }

                localAddon.Url = remoteAddon.Url;

                if (localAddon.Md5 == remoteAddon.Md5)
                {
                    validFiles.Add(localAddon);
                    progress();
                    continue;
                }

                updateFiles.Add(localAddon);
                progress();
            }
        }

        private static async Task<List<Addon>> GetValid()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var valid = File.ReadAllText(Path.Combine(Settings.Default.LocalFolder, "md5", "valid.json"));
                    return JsonConvert.DeserializeObject<List<Addon>>(valid);
                }
                catch (Exception)
                {
                    return new List<Addon>();
                }
            });
        }

        private static Task SaveValid(IEnumerable<Addon> valid)
        {
            return Task.Run(() =>
                File.WriteAllText(Path.Combine(Settings.Default.LocalFolder, "md5", "valid.json"),
                    JsonConvert.SerializeObject(valid)));
        }

        private static async Task<List<string>> GetMods()
        {
            var mods = new List<string>();

            var settings = new XmlReaderSettings {Async = true};

            using (var fileStream =
                File.OpenRead(Path.Combine(Settings.Default.LocalFolder, "md5", "autoconfig", "Mods.xml")))
            using (var reader = XmlReader.Create(fileStream, settings))
            {
                while (await reader.ReadAsync())
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "Name")
                        mods.Add(await reader.ReadElementContentAsStringAsync());
            }

            return mods;
        }

        private static async Task<List<Addon>> GetAddons()
        {
            var addons = new List<Addon>();

            var settings = new XmlReaderSettings {Async = true};

            using (var fileStream =
                File.OpenRead(Path.Combine(Settings.Default.LocalFolder, "md5", "autoconfig", "Addons.xml")))
            using (var reader = XmlReader.Create(fileStream, settings))
            {
                while (await reader.ReadAsync())
                {
                    if (reader.NodeType != XmlNodeType.Element || reader.Name != "Addons") continue;
                    var addon = new Addon();

                    do
                    {
                        await reader.ReadAsync();
                        if (reader.NodeType != XmlNodeType.Element) continue;

                        switch (reader.Name)
                        {
                            case "Md5":
                                addon.Md5 = await reader.ReadElementContentAsStringAsync();
                                break;
                            case "Path":
                                addon.Path = await reader.ReadElementContentAsStringAsync();
                                break;
                            case "Pbo":
                                addon.Pbo = await reader.ReadElementContentAsStringAsync();
                                break;
                            case "Size":
                                addon.Size = reader.ReadElementContentAsLong();
                                break;
                            case "Url":
                                addon.Url = await reader.ReadElementContentAsStringAsync();
                                break;
                        }
                    } while (!(reader.NodeType == XmlNodeType.EndElement && reader.Name == "Addons"));

                    addons.Add(addon);
                }
            }

            return addons;
        }

        private static void UnpackFile(string path)
        {
            using (var archive = SevenZipArchive.Open(path))
            {
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                    entry.WriteToDirectory(Path.GetDirectoryName(path), new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
            }

            if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
            }
        }

        private static string CalculateMd5(string fileName, long fileLength)
        {
            using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
                (int) Math.Min(64 * 1024, fileLength)))
            {
                var hash = Md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
            }
        }
    }
}