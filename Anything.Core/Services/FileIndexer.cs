using Anything.Core.Abstractions;
using Anything.Core.Models;
using System.Collections.Concurrent;

namespace Anything.Core.Services;

public sealed class FileIndexer : IFileIndexProvider, IFileSystemChangeMonitor
{
    private readonly ConcurrentDictionary<string, FileEntry> _index = new();
    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly PluginManager? _pluginManager;
    private bool _isBuilding;

    public FileIndexer(PluginManager? pluginManager = null)
    {
        _pluginManager = pluginManager;
    }

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

        try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "anything-index.log"), "FileIndexer: Starting to build index...\n"); } catch { }

        var roots = GetSearchRoots();

        try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "anything-index.log"), $"FileIndexer: Found {roots.Count()} roots to index\n"); } catch { }

        var tasks = roots.Select(root =>
        {
            try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "anything-index.log"), $"FileIndexer: Indexing {root}\n"); } catch { }
            return Task.Run(() => IndexDirectory(root, cancellationToken), cancellationToken);
        });
        await Task.WhenAll(tasks);

        try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "anything-index.log"), $"FileIndexer: Index build complete. Total files: {_index.Count}\n"); } catch { }

        SetupFileWatchers(roots);
        _isBuilding = false;
    }

    private IEnumerable<string> GetSearchRoots()
    {
        if (OperatingSystem.IsWindows())
        {
            // Only index user directories, not all drives
            return new List<string>
            {
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };
        }

        // Linux/macOS - only index home directory
        return new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };
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
                AddDirToIndex(current);

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

    private void AddDirToIndex(string dirPath)
    {
        try
        {
            var info = new DirectoryInfo(dirPath);
            var entry = new FileEntry
            {
                Path = info.FullName,
                Name = info.Name,
                Size = 0,
                LastModifiedUtc = info.LastWriteTimeUtc,
                IsDirectory = true
            };

            _index[info.FullName] = entry;
        }
        catch
        {
            // Ignore errors
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

    public Task<IEnumerable<FileEntry>> SearchAsync(string query, SearchOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new SearchOptions();
        query = query.Trim();

        if (string.IsNullOrEmpty(query))
            return Task.FromResult<IEnumerable<FileEntry>>(Array.Empty<FileEntry>());

        var comparison = options.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var results = _index.Values.AsEnumerable();

        if (options.UseRegex)
        {
            var regex = new System.Text.RegularExpressions.Regex(query,
                options.MatchCase ? System.Text.RegularExpressions.RegexOptions.None : System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            results = results.Where(e => regex.IsMatch(e.Name) || (options.MatchPath && regex.IsMatch(e.Path)));
        }
        else if (options.MatchWholeWord)
        {
            results = results.Where(e =>
                e.Name.Split(' ').Any(w => w.Equals(query, comparison)) ||
                (options.MatchPath && e.Path.Split('\\', '/').Any(w => w.Equals(query, comparison))));
        }
        else
        {
            results = results.Where(e =>
                e.Name.Contains(query, comparison) ||
                (options.MatchPath && e.Path.Contains(query, comparison)));
        }

        if (options.TypeFilter == FilterType.FilesOnly)
            results = results.Where(e => !e.IsDirectory);
        else if (options.TypeFilter == FilterType.FoldersOnly)
            results = results.Where(e => e.IsDirectory);

        if (options.MinSize.HasValue)
            results = results.Where(e => e.Size >= options.MinSize.Value);
        if (options.MaxSize.HasValue)
            results = results.Where(e => e.Size <= options.MaxSize.Value);

        if (options.MinDate.HasValue)
            results = results.Where(e => e.LastModifiedUtc >= options.MinDate.Value);
        if (options.MaxDate.HasValue)
            results = results.Where(e => e.LastModifiedUtc <= options.MaxDate.Value);

        var final = results
            .Take(options.MaxResults)
            .ToArray();

        if (_pluginManager != null)
            return _pluginManager.ApplyPluginsAsync(query, options, final);

        return Task.FromResult<IEnumerable<FileEntry>>(final);
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
