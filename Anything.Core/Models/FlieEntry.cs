namespace Anything.Core.Models;

public sealed class FileEntry
{
    public required string Path { get; init; }
    public required string Name { get; init; }
    public long Size { get; init; }
    public DateTime LastModifiedUtc { get; init; }
}
