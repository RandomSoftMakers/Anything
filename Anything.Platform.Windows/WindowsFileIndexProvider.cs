using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Anything.Core.Abstractions;
using Anything.Core.Models;

namespace Anything.Platform.Windows;

public sealed class WindowsFileIndexProvider : IFileIndexProvider
{
    private readonly List<FileEntry> _entries = new();

    public async Task BuildInitialIndexAsync(CancellationToken cancellationToken = default)
    {
        _entries.Clear();

        var drives = DriveInfo
            .GetDrives()
            .Where(d => d.IsReady)
            .ToArray();

        foreach (var drive in drives)
        {
            string root = drive.RootDirectory.FullName;

            await Task.Run(() =>
            {
                foreach (var entry in SafeEnumerateAll(root, cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    _entries.Add(entry);
                }
            }, cancellationToken);
        }
    }

    public Task<IEnumerable<FileEntry>> SearchAsync(string query, SearchOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new SearchOptions();
        query = query.Trim();

        if (string.IsNullOrEmpty(query))
            return Task.FromResult<IEnumerable<FileEntry>>(Array.Empty<FileEntry>());

        var comparison = options.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var results = _entries.AsEnumerable();

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

        return Task.FromResult(results.Take(options.MaxResults).ToArray() as IEnumerable<FileEntry>);
    }

    private static IEnumerable<FileEntry> SafeEnumerateAll(string root, CancellationToken cancellationToken)
    {
        var dirs = new Stack<string>();
        dirs.Push(root);

        while (dirs.Count > 0)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            string current = dirs.Pop();

            var dirInfo = TryGetDirInfo(current);
            if (dirInfo != null)
            {
                yield return new FileEntry
                {
                    Path = dirInfo.FullName,
                    Name = dirInfo.Name,
                    Size = 0,
                    LastModifiedUtc = dirInfo.LastWriteTimeUtc,
                    IsDirectory = true
                };
            }

            string[] subDirs = TryGetDirectories(current);
            string[] files = TryGetFiles(current);

            foreach (var f in files)
            {
                var info = TryGetFileInfo(f);
                if (info != null)
                {
                    yield return new FileEntry
                    {
                        Path = info.FullName,
                        Name = info.Name,
                        Size = info.Length,
                        LastModifiedUtc = info.LastWriteTimeUtc
                    };
                }
            }

            foreach (var d in subDirs)
                dirs.Push(d);
        }
    }

    private static DirectoryInfo? TryGetDirInfo(string path)
    {
        try { return new DirectoryInfo(path); }
        catch { return null; }
    }

    private static FileInfo? TryGetFileInfo(string path)
    {
        try { return new FileInfo(path); }
        catch { return null; }
    }

    private static string[] TryGetDirectories(string path)
    {
        try { return Directory.GetDirectories(path); }
        catch { return Array.Empty<string>(); }
    }

    private static string[] TryGetFiles(string path)
    {
        try { return Directory.GetFiles(path); }
        catch { return Array.Empty<string>(); }
    }
}
