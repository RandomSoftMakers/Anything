namespace Anything.Core.Models;

public sealed class SearchOptions
{
    public bool MatchCase { get; set; }
    public bool MatchWholeWord { get; set; }
    public bool MatchPath { get; set; }
    public bool UseRegex { get; set; }
    public FilterType TypeFilter { get; set; } = FilterType.All;
    public long? MinSize { get; set; }
    public long? MaxSize { get; set; }
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    public int MaxResults { get; set; } = 500;
}

public enum FilterType
{
    All,
    FilesOnly,
    FoldersOnly
}