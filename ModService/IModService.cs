using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace arma_launcher.ModService
{
    public interface IModService
    {
        Task Download(IEnumerable<Addon> downloadFiles, IProgress<ProgressMessage> progress,
            CancellationToken cancellation);

        Task<(IEnumerable<Addon> validFiles, IEnumerable<Addon> updateFiles, IEnumerable<Addon> newFiles,
            IEnumerable<Addon> deleteFiles)> Validate(bool full, IProgress<ProgressMessage> progress);
    }
}