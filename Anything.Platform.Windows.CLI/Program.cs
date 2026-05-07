using Anything.Core.Cli;
using Anything.Core.Services;
using Anything.Indexer.Daemon;
using Anything.Platform.Windows;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h" or "/?")
        {
            CliParser.PrintHelp("anything");
            return args.Length == 0 ? 1 : 0;
        }

        if (args[0] is "--version" or "-v")
        {
            Console.WriteLine("Anything CLI 1.0.0 (windows)");
            return 0;
        }

        var (query, options, flags) = CliParser.Parse(args);

        if (query is null)
        {
            Console.Error.WriteLine("Error: search query is required");
            CliParser.PrintHelp("anything");
            return 1;
        }

        // Try connecting to the indexer daemon first
        try
        {
            var client = new IndexerClient();
            if (await client.PingAsync())
            {
                Console.Error.WriteLine("Connected to indexer daemon");
                var results = await client.SearchAsync(query, options);
                CliParser.PrintResults(results, flags);
                return 0;
            }
        }
        catch
        {
            // Daemon not available, fall back to direct indexing
        }

        Console.Error.WriteLine("Indexer daemon not found. Building index in-process...");
        var provider = new WindowsFileIndexProvider();
        var service = new AnythingSearchService(provider);

        await service.BuildIndexAsync();

        var directResults = await service.SearchAsync(query, options);
        CliParser.PrintResults(directResults, flags);
        return 0;
    }
}
