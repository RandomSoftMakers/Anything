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
            try
            {
                foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
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
                    catch
                    {
                        // игнорируем ошибки доступа
                    }
                }
            }
            catch
            {
                // игнорируем ошибки верхнего уровня
            }
        }, cancellationToken);
    }

    public Task<IEnumerable<FileEntry>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        query = query.Trim();
        IEnumerable<FileEntry> result = _entries
            .Where(e => e.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return Task.FromResult(result);
    }
}
