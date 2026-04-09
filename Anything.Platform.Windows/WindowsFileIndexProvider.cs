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
                foreach (var file in SafeEnumerateFiles(root, cancellationToken))
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
                        // игнорируем AccessDenied и прочее
                    }
                }
            }, cancellationToken);
        }
    }

    public Task<IEnumerable<FileEntry>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        query = query.Trim();
        IEnumerable<FileEntry> result = _entries
            .Where(e => e.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return Task.FromResult(result);
    }

    private static IEnumerable<string> SafeEnumerateFiles(string root, CancellationToken cancellationToken)
    {
        var dirs = new Stack<string>();
        dirs.Push(root);

        while (dirs.Count > 0)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            string current = dirs.Pop();

            string[] subDirs = Array.Empty<string>();
            string[] files = Array.Empty<string>();

            try { subDirs = Directory.GetDirectories(current); } catch { }
            try { files = Directory.GetFiles(current); } catch { }

            foreach (var f in files)
                yield return f;

            foreach (var d in subDirs)
                dirs.Push(d);
        }
    }
}
