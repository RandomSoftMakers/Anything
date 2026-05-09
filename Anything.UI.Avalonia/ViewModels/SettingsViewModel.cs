using System.Collections.ObjectModel;
using System.Linq;
using Anything.Core.Services;
using Anything.UI.Avalonia.Settings;

namespace Anything.UI.Avalonia.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private int _selectedThemeIndex;
    private int _selectedLanguageIndex;
    private string _maxResults = "500";
    private bool _enableIndexer;
    private bool _indexerAutoStart;
    private bool _isIndexerRunning;

    public int SelectedThemeIndex
    {
        get => _selectedThemeIndex;
        set => SetProperty(ref _selectedThemeIndex, value);
    }

    public int SelectedLanguageIndex
    {
        get => _selectedLanguageIndex;
        set => SetProperty(ref _selectedLanguageIndex, value);
    }

    public string MaxResults
    {
        get => _maxResults;
        set => SetProperty(ref _maxResults, value);
    }

    public bool EnableIndexer
    {
        get => _enableIndexer;
        set => SetProperty(ref _enableIndexer, value);
    }

    public bool IndexerAutoStart
    {
        get => _indexerAutoStart;
        set => SetProperty(ref _indexerAutoStart, value);
    }

    public bool IsIndexerRunning
    {
        get => _isIndexerRunning;
        set
        {
            if (SetProperty(ref _isIndexerRunning, value))
                OnPropertyChanged(nameof(IndexerStatusText));
        }
    }

    public ObservableCollection<PluginEntryViewModel> Plugins { get; } = new();
    public bool HasPlugins => Plugins.Count > 0;
    public bool ShowIndexerSection =>
#if NO_INDEXER_DAEMON
        false;
#else
        true;
#endif

    public string IndexerLabel => Lang.T("Indexer");
    public string IndexerStatusLabel => Lang.T("IndexerStatus");
    public string IndexerStatusText => IsIndexerRunning ? Lang.T("IndexerRunning") : Lang.T("IndexerStopped");
    public string EnableIndexerLabel => Lang.T("EnableIndexer");
    public string IndexerAutoStartLabel => Lang.T("IndexerAutoStart");

    public SettingsViewModel()
    {
        var settings = SettingsManager.Current;
        _selectedThemeIndex = AppSettings.ThemeIndex(settings.Theme);
        _selectedLanguageIndex = AppSettings.LanguageIndex(settings.Language);
        _maxResults = settings.MaxResults.ToString();
        _enableIndexer = settings.EnableIndexer;
        _indexerAutoStart = settings.IndexerAutoStart;

        LoadPlugins();
        _ = CheckIndexerStatusAsync();
    }

    private void LoadPlugins()
    {
        var pm = TryGetPluginManager();
        if (pm == null) return;

        Plugins.Clear();
        foreach (var plugin in pm.Plugins)
            Plugins.Add(new PluginEntryViewModel(plugin));

        OnPropertyChanged(nameof(HasPlugins));
    }

    private static PluginManager? TryGetPluginManager()
    {
        if (App.Current is App app && app.PluginManager != null)
            return app.PluginManager;
        return null;
    }

    private async Task CheckIndexerStatusAsync()
    {
#if !NO_INDEXER_DAEMON
        try
        {
            var client = new Anything.Indexer.Daemon.IndexerClient();
            IsIndexerRunning = await client.PingAsync();
        }
        catch
        {
            IsIndexerRunning = false;
        }
#endif
    }

    public void Save()
    {
        var settings = SettingsManager.Current;
        settings.Theme = AppSettings.ThemeName(SelectedThemeIndex);
        settings.Language = AppSettings.LanguageCode(SelectedLanguageIndex);
        settings.EnableIndexer = EnableIndexer;
        settings.IndexerAutoStart = IndexerAutoStart;

        if (int.TryParse(MaxResults, out int maxResults))
            settings.MaxResults = maxResults;

        SettingsManager.Save();
        App.ApplyTheme(settings.Theme);
    }
}
