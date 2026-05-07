using Anything.Core.Models;

namespace Anything.UI.Avalonia.ViewModels;

public class FileEntryViewModel
{
    public string Name { get; }
    public string Path { get; }
    public long Size { get; }
    public DateTime LastModifiedUtc { get; }
    public bool IsDirectory { get; }
    public string Extension { get; }

    public string Icon => IsDirectory ? "\U0001F4C1" : "\U0001F4C4";
    public string SizeText => IsDirectory ? "" : FormatSize(Size);

    public FileEntryViewModel(FileEntry entry)
    {
        Name = entry.Name;
        Path = entry.Path;
        Size = entry.Size;
        LastModifiedUtc = entry.LastModifiedUtc;
        IsDirectory = entry.IsDirectory;
        Extension = entry.Extension;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}
