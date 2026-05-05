using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Anything.Core.Services;
using Anything.UI.Avalonia.Views;
using Anything.UI.Avalonia.ViewModels;
using Anything.UI.Avalonia.Settings;

namespace Anything.UI.Avalonia;

public partial class App : Application
{
    public static ThemeVariant CurrentThemeVariant { get; private set; } = ThemeVariant.Dark;

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
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = new Views.MainWindow();
                desktop.MainWindow = mainWindow;

                // Create and initialize the search service
                var indexer = new Anything.Core.Services.FileIndexer();
                var searchService = new AnythingSearchService(indexer);
                var viewModel = new MainViewModel(searchService);
                mainWindow.DataContext = viewModel;

                // Initialize async (build index) without blocking startup
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await viewModel.InitializeAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error initializing search: {ex.Message}");
                    }
                });

                // Show first run window if needed (after main window is ready)
                if (SettingsManager.Current.IsFirstRun)
                {
                    mainWindow.Loaded += (s, e) =>
                    {
                        var firstRunWindow = new Views.FirstRunWindow();
                        firstRunWindow.ShowDialog(mainWindow);
                    };
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fatal error during startup: {ex}");
            File.AppendAllText(Path.Combine(Path.GetTempPath(), "anything-fatal.log"), $"Fatal error: {ex}\n");
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static void ApplyTheme(string themeName)
    {
        CurrentThemeVariant = themeName == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        var styles = Current?.Styles;
        if (styles != null)
        {
            // Remove old theme styles
            var oldTheme = styles.FirstOrDefault(s => s is StyleInclude);
            if (oldTheme != null)
            {
                styles.Remove(oldTheme);
            }

            // Add new theme
            var newTheme = new StyleInclude(new Uri("avares://Anything.UI.Avalonia/"))
            {
                Source = new Uri($"avares://Anything.UI.Avalonia/Themes/{themeName}.axaml")
            };
            styles.Add(newTheme);
        }
    }
}
