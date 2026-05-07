using System.Text.Json.Serialization;
using Anything.Core.Models;

namespace Anything.Indexer.Daemon;

public sealed class IndexerRequest
{
    public string Action { get; set; } = "";
    public string Query { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int MaxResults { get; set; } = 500;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool MatchCase { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool MatchWholeWord { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool MatchPath { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool UseRegex { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int TypeFilter { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long? MinSize { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long? MaxSize { get; set; }

    public SearchOptions ToSearchOptions() => new()
    {
        MaxResults = MaxResults,
        MatchCase = MatchCase,
        MatchWholeWord = MatchWholeWord,
        MatchPath = MatchPath,
        UseRegex = UseRegex,
        TypeFilter = (FilterType)TypeFilter,
        MinSize = MinSize,
        MaxSize = MaxSize
    };
}

public sealed class IndexerResponse
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<FileEntryDto> Results { get; set; } = new();

    public static IndexerResponse CreateSuccess(List<FileEntryDto> results) => new()
    {
        IsSuccess = true,
        Results = results
    };

    public static IndexerResponse CreateError(string message) => new()
    {
        IsSuccess = false,
        ErrorMessage = message
    };
}

public sealed class FileEntryDto
{
    public string Path { get; set; } = "";
    public string Name { get; set; } = "";
    public long Size { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public bool IsDirectory { get; set; }

    public FileEntry ToFileEntry() => new()
    {
        Path = Path,
        Name = Name,
        Size = Size,
        LastModifiedUtc = LastModifiedUtc,
        IsDirectory = IsDirectory
    };
}
