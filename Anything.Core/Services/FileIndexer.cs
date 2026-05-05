using Anything.Core.Abstractions;
using Anything.Core.Models;
using System.Collections.Concurrent;

namespace Anything.Core.Services;

public sealed class FileIndexer : IFileIndexProvider, IFileSystemChangeMonitor
{
    private readonly ConcurrentDictionary<string, FileEntry> _index = new();
    private readonly List<FileSystemWatcher> _watchers = new();
    private bool _isBuilding;

    public event EventHandler<FileEntry>? FileCreated;
    public event EventHandler<FileEntry>? FileDeleted;
    public event EventHandler<FileEntry>? FileChanged;
    public event EventHandler<(FileEntry OldEntry, FileEntry NewEntry)>? FileRenamed;

    public async Task BuildInitialIndexAsync(CancellationToken cancellationToken = default)
    {
        if (_isBuilding)
            return;

        _isBuilding = true;
        _index.Clear();

        File.AppendAllText("/tmp/anything-index.log", "FileIndexer: Starting to build index...\n");

        var roots = GetSearchRoots();

        File.AppendAllText("/tmp/anything-index.log", $"FileIndexer: Found {roots.Count()} roots to index\n");

        var tasks = roots.Select(root =>
        {
            File.AppendAllText("/tmp/anything-index.log", $"FileIndexer: Indexing {root}\n");
            return Task.Run(() => IndexDirectory(root, cancellationToken), cancellationToken);
        });
        await Task.WhenAll(tasks);

        File.AppendAllText("/tmp/anything-index.log", $"FileIndexer: Index build complete. Total files: {_index.Count}\n");

        SetupFileWatchers(roots);
        _isBuilding = false;
    }

    private IEnumerable<string> GetSearchRoots()
    {
        if (OperatingSystem.IsWindows())
        {
            return DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d => d.RootDirectory.FullName);
        }

        // Linux/macOS - index home directory and common locations
        var paths = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };

        // Add common directories if they exist
        var commonPaths = new[]
        {
            "/home",
            "/opt",
            "/usr/local"
        };

        foreach (var path in commonPaths)
        {
            if (Directory.Exists(path))
                paths.Add(path);
        }

        return paths;
    }

    private void IndexDirectory(string root, CancellationToken cancellationToken)
    {
        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            var current = stack.Pop();

            try
            {
                foreach (var file in Directory.GetFiles(current))
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    AddFileToIndex(file);
                }

                foreach (var dir in Directory.GetDirectories(current))
                {
                    stack.Push(dir);
                }
            }
            catch
            {
                // Ignore access denied errors
            }
        }
    }

    private void AddFileToIndex(string filePath)
    {
        try
        {
            var info = new FileInfo(filePath);
            var entry = new FileEntry
            {
                Path = info.FullName,
                Name = info.Name,
                Size = info.Length,
                LastModifiedUtc = info.LastWriteTimeUtc
            };

            _index[info.FullName] = entry;
        }
        catch
        {
            // Ignore errors
        }
    }

    public Task<IEnumerable<FileEntry>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        query = query.Trim();

        if (string.IsNullOrEmpty(query))
            return Task.FromResult<IEnumerable<FileEntry>>(Array.Empty<FileEntry>());

        var results = _index.Values
            .Where(e => e.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(500)
            .ToArray();

        return Task.FromResult<IEnumerable<FileEntry>>(results);
    }

    private void SetupFileWatchers(IEnumerable<string> roots)
    {
        foreach (var root in roots)
        {
            try
            {
                var watcher = new FileSystemWatcher(root)
                {
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
                };

                watcher.Created += (s, e) => OnFileCreated(e.FullPath);
                watcher.Deleted += (s, e) => OnFileDeleted(e.FullPath);
                watcher.Changed += (s, e) => OnFileChanged(e.FullPath);
                watcher.Renamed += (s, e) => OnFileRenamed(e.OldFullPath, e.FullPath);

                _watchers.Add(watcher);
            }
            catch
            {
                // Ignore errors setting up watcher
            }
        }
    }

    private void OnFileCreated(string path)
    {
        if (File.Exists(path))
        {
            AddFileToIndex(path);
            var entry = _index.Values.FirstOrDefault(e => e.Path == path);
            if (entry != null)
                FileCreated?.Invoke(this, entry);
        }
    }

    private void OnFileDeleted(string path)
    {
        if (_index.TryRemove(path, out var entry))
        {
            FileDeleted?.Invoke(this, entry);
        }
    }

    private void OnFileChanged(string path)
    {
        if (_index.TryGetValue(path, out var oldEntry))
        {
            try
            {
                var info = new FileInfo(path);
                var newEntry = new FileEntry
                {
                    Path = info.FullName,
                    Name = info.Name,
                    Size = info.Length,
                    LastModifiedUtc = info.LastWriteTimeUtc
                };

                _index[path] = newEntry;
                FileChanged?.Invoke(this, newEntry);
            }
            catch
            {
                // Ignore errors
            }
        }
    }

    private void OnFileRenamed(string oldPath, string newPath)
    {
        if (_index.TryRemove(oldPath, out var oldEntry))
        {
            AddFileToIndex(newPath);
            var newEntry = _index.Values.FirstOrDefault(e => e.Path == newPath);
            if (newEntry != null)
                FileRenamed?.Invoke(this, (oldEntry, newEntry));
        }
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        // Watchers are already started in BuildInitialIndexAsync
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }
        _watchers.Clear();
        return ValueTask.CompletedTask;
    }
}
