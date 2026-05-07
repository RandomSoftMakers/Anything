using Anything.Core.Models;

namespace Anything.Core.Cli;

public sealed record CliFlags
{
    public bool ShowCount { get; init; }
    public bool PathOnly { get; init; }
}

public static class CliParser
{
    public static void PrintHelp(string toolName)
    {
        Console.WriteLine($"Usage: {toolName} [options] <query>");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --help, -h          Show help");
        Console.WriteLine("  --version, -v       Show version");
        Console.WriteLine("  --count, -c         Only show result count");
        Console.WriteLine("  --path-only, -p     Only show file paths");
        Console.WriteLine("  --regex, -r         Treat query as regex");
        Console.WriteLine("  --match-case        Case-sensitive search");
        Console.WriteLine("  --whole-word        Match whole words only");
        Console.WriteLine("  --match-path        Search in full path too");
        Console.WriteLine("  --type <file|dir>   Filter by type");
        Console.WriteLine("  --min-size <bytes>  Minimum file size");
        Console.WriteLine("  --max-size <bytes>  Maximum file size");
        Console.WriteLine("  --min-date <yyyy-MM-dd>  Minimum modified date");
        Console.WriteLine("  --max-date <yyyy-MM-dd>  Maximum modified date");
        Console.WriteLine("  --max-results <n>   Max results (default 500)");
    }

    public static (string? Query, SearchOptions Options, CliFlags Flags) Parse(string[] args)
    {
        var options = new SearchOptions();
        var flags = new CliFlags();
        var remaining = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--count" or "-c":       flags = flags with { ShowCount = true }; break;
                case "--path-only" or "-p":   flags = flags with { PathOnly = true }; break;
                case "--regex" or "-r":       options.UseRegex = true; break;
                case "--match-case":          options.MatchCase = true; break;
                case "--whole-word":          options.MatchWholeWord = true; break;
                case "--match-path":          options.MatchPath = true; break;
                case "--type":
                    if (++i < args.Length)
                        options.TypeFilter = args[i] == "dir" ? FilterType.FoldersOnly : FilterType.FilesOnly;
                    break;
                case "--min-size":
                    if (++i < args.Length && long.TryParse(args[i], out var min)) options.MinSize = min;
                    break;
                case "--max-size":
                    if (++i < args.Length && long.TryParse(args[i], out var max)) options.MaxSize = max;
                    break;
                case "--min-date":
                    if (++i < args.Length && DateTime.TryParse(args[i], out var minD)) options.MinDate = minD;
                    break;
                case "--max-date":
                    if (++i < args.Length && DateTime.TryParse(args[i], out var maxD)) options.MaxDate = maxD;
                    break;
                case "--max-results":
                    if (++i < args.Length && int.TryParse(args[i], out var mr)) options.MaxResults = mr;
                    break;
                default:
                    remaining.Add(args[i]);
                    break;
            }
        }

        var query = remaining.Count > 0 ? string.Join(" ", remaining) : null;
        return (query, options, flags);
    }

    public static void PrintResults(IEnumerable<FileEntry> results, CliFlags flags)
    {
        int count = 0;
        foreach (var entry in results)
        {
            count++;
            if (flags.PathOnly)
            {
                Console.WriteLine(entry.Path);
            }
            else
            {
                var type = entry.IsDirectory ? 'd' : ' ';
                Console.WriteLine($"[{type}] {entry.Name}");
                Console.WriteLine($"  {entry.Path}");
                Console.WriteLine($"  {FormatSize(entry.Size)}  {entry.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();
            }
        }

        if (flags.ShowCount)
            Console.Error.WriteLine($"Found: {count} results");

        if (count == 0)
            Console.Error.WriteLine("No results.");
    }

    public static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}