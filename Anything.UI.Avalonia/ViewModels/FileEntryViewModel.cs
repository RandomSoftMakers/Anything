using Anything.Core.Models;

namespace Anything.UI.Avalonia.ViewModels;

public class FileEntryViewModel
{
    public string Name { get; }
    public string Path { get; }
    public long Size { get; }
    public DateTime LastModifiedUtc { get; }

    public FileEntryViewModel(FileEntry entry)
    {
        Name = entry.Name;
        Path = entry.Path;
        Size = entry.Size;
        LastModifiedUtc = entry.LastModifiedUtc;
    }
}
