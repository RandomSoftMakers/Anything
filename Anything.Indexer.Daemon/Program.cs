using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Anything.Indexer.Daemon;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length > 0 && args[0] is "--help" or "-h")
        {
            Console.WriteLine("anything-indexer - Background file indexer daemon");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --help, -h     Show help");
            Console.WriteLine("  --install      Register as Windows service");
            Console.WriteLine("  --uninstall    Unregister Windows service");
            Console.WriteLine();
            Console.WriteLine("Without arguments, runs as a foreground process.");
            Console.WriteLine("On Windows, use --install to register as a service.");
            Console.WriteLine("On Linux, use the provided systemd unit file.");
            return 0;
        }

        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSingleton<FileIndexerDaemon>();
        builder.Services.AddHostedService<IndexerHostedService>();

        builder.Logging.ClearProviders();
#pragma warning disable CA1416
        if (OperatingSystem.IsWindows())
        {
            builder.Logging.AddEventLog(settings =>
            {
                settings.SourceName = "Anything Indexer";
                settings.LogName = "Application";
            });
        }
#pragma warning restore CA1416

        if (OperatingSystem.IsLinux())
        {
            builder.Services.AddSystemd();
        }

        var host = builder.Build();
        await host.RunAsync();
        return 0;
    }
}
