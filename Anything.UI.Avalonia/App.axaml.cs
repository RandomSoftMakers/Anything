using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Anything.Core.Abstractions;
using Anything.Core.Services;
#if !NO_INDEXER_DAEMON
using Anything.Indexer.Daemon;
#endif
using Anything.UI.Avalonia.Views;
using Anything.UI.Avalonia.ViewModels;
using Anything.UI.Avalonia.Settings;

namespace Anything.UI.Avalonia;

public partial class App : Application
{
    public PluginManager? PluginManager { get; private set; }
    public IFileIndexProvider? FileIndexProvider { get; private set; }

    private async Task<IFileIndexProvider> CreateIndexProviderAsync()
    {
        var settings = SettingsManager.Current;

#if !NO_INDEXER_DAEMON
        if (settings.EnableIndexer)
        {
            try
            {
                var client = new IndexerClient();
                if (await client.PingAsync())
                {
                    System.Diagnostics.Debug.WriteLine("Connected to indexer daemon");
                    return new DaemonFileIndexProvider(client);
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Indexer daemon not reachable, using in-process indexing");
            }
        }
#endif

        System.Diagnostics.Debug.WriteLine("Using in-process file indexer");
        var indexer = new FileIndexer(PluginManager);
        await indexer.BuildInitialIndexAsync();
        return indexer;
    }

    public override void Initialize()
    {
        SettingsManager.Load();
        AvaloniaXamlLoader.Load(this);
        ApplyTheme(SettingsManager.Current.Theme);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            PluginManager = new PluginManager();
            var pluginsDir = Path.Combine(AppContext.BaseDirectory, "plugins");
            PluginManager.LoadFromDirectory(pluginsDir);
            _ = PluginManager.LoadAllAsync();

            var searchService = new AnythingSearchService(new FileIndexer(PluginManager));
            var viewModel = new MainViewModel(searchService);

            _ = InitializeIndexerAsync(viewModel);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = new Views.MainWindow
                {
                    DataContext = viewModel
                };
                desktop.MainWindow = mainWindow;

                if (SettingsManager.Current.IsFirstRun)
                {
                    mainWindow.Loaded += (s, e) =>
                    {
                        var firstRunWindow = new Views.FirstRunWindow();
                        firstRunWindow.ShowDialog(mainWindow);
                    };
                }
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            {
                var mainView = new Views.MainView
                {
                    DataContext = viewModel
                };
                singleView.MainView = mainView;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fatal error during startup: {ex}");
            try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "anything-fatal.log"), $"Fatal error: {ex}\n"); } catch { }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeIndexerAsync(MainViewModel viewModel)
    {
        try
        {
            var provider = await CreateIndexProviderAsync();
            FileIndexProvider = provider;

#if !NO_INDEXER_DAEMON
            if (provider is DaemonFileIndexProvider)
            {
                await viewModel.SetSearchServiceAsync(new AnythingSearchService(provider));
                return;
            }
#endif
            await viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Indexer init error: {ex.Message}");
            try { await viewModel.InitializeAsync(); } catch { }
        }
    }

    public static readonly string[] ThemeNames =
    [
        "Dark", "Light",
        "CatppuccinMocha", "CatppuccinLatte",
        "SolarizedDark", "SolarizedLight",
        "VSCodeDark", "GNOME",
        "BreezeDark", "BreezeLight",
        "LiquidGlass"
    ];

    public static bool IsDarkTheme(string name) => name switch
    {
        "Dark" or "CatppuccinMocha" or "SolarizedDark" or "VSCodeDark" or "BreezeDark" or "LiquidGlass" => true,
        _ => false
    };

    public static void ApplyTheme(string themeName)
    {
        var styles = Current?.Styles;
        if (styles != null)
        {
            var oldTheme = styles.FirstOrDefault(s => s is StyleInclude);
            if (oldTheme != null)
                styles.Remove(oldTheme);

            var newTheme = new StyleInclude(new Uri("avares://Anything.UI.Avalonia/"))
            {
                Source = new Uri($"avares://Anything.UI.Avalonia/Themes/{themeName}.axaml")
            };
            styles.Add(newTheme);
        }
    }
}
