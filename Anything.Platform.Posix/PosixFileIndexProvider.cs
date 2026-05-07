using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Anything.Core.Abstractions;
using Anything.Core.Models;

namespace Anything.Platform.Posix;

public sealed class PosixFileIndexProvider : IFileIndexProvider
{
    private readonly List<FileEntry> _entries = new();
    private readonly string _root;

    public PosixFileIndexProvider(string root = "/")
    {
        _root = root;
    }

    public async Task BuildInitialIndexAsync(CancellationToken cancellationToken = default)
    {
        _entries.Clear();

        await Task.Run(() =>
        {
            var dirs = new Stack<string>();
            dirs.Push(_root);

            while (dirs.Count > 0 && !cancellationToken.IsCancellationRequested)
            {
                var current = dirs.Pop();

                try
                {
                    var dirInfo = new DirectoryInfo(current);
                    _entries.Add(new FileEntry
                    {
                        Path = dirInfo.FullName,
                        Name = dirInfo.Name,
                        Size = 0,
                        LastModifiedUtc = dirInfo.LastWriteTimeUtc,
                        IsDirectory = true
                    });

                    foreach (var file in Directory.EnumerateFiles(current))
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        try
                        {
                            var info = new FileInfo(file);
                            _entries.Add(new FileEntry
                            {
                                Path = info.FullName,
                                Name = info.Name,
                                Size = info.Length,
                                LastModifiedUtc = info.LastWriteTimeUtc
                            });
                        }
                        catch { }
                    }

                    foreach (var dir in Directory.EnumerateDirectories(current))
                    {
                        dirs.Push(dir);
                    }
                }
                catch { }
            }
        }, cancellationToken);
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
                (options.MatchPath && e.Path.Split('/', '\\').Any(w => w.Equals(query, comparison))));
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
}
