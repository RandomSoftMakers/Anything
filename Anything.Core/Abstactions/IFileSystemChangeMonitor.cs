using Anything.Core.Models;

namespace Anything.Core.Abstractions;

public interface IFileSystemChangeMonitor : IAsyncDisposable
{
    event EventHandler<FileEntry>? FileCreated;
    event EventHandler<FileEntry>? FileDeleted;
    event EventHandler<FileEntry>? FileChanged;
    event EventHandler<(FileEntry OldEntry, FileEntry NewEntry)>? FileRenamed;

    Task StartAsync(CancellationToken cancellationToken = default);
}
