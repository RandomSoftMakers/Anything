using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Anything.Core.Models;
using Anything.Core.Services;
using Anything.UI.Avalonia.Settings;

namespace Anything.UI.Avalonia.ViewModels;

public class MainViewModel : ViewModelBase
{
    private AnythingSearchService? _searchService;
    private string _query = string.Empty;
    private bool _matchCase;
    private bool _matchWholeWord;
    private bool _matchPath;
    private bool _useRegex;
    private int _typeFilterIndex;
    private string _minSize = "";
    private string _maxSize = "";
    private bool _showFilters;

    public ObservableCollection<FileEntryViewModel> Results { get; } = new();

    public string Query
    {
        get => _query;
        set
        {
            if (SetProperty(ref _query, value))
                _ = SearchAsync(_query);
        }
    }

    public bool MatchCase
    {
        get => _matchCase;
        set { SetProperty(ref _matchCase, value); _ = SearchAsync(_query); }
    }

    public bool MatchWholeWord
    {
        get => _matchWholeWord;
        set { SetProperty(ref _matchWholeWord, value); _ = SearchAsync(_query); }
    }

    public bool MatchPath
    {
        get => _matchPath;
        set { SetProperty(ref _matchPath, value); _ = SearchAsync(_query); }
    }

    public bool UseRegex
    {
        get => _useRegex;
        set { SetProperty(ref _useRegex, value); _ = SearchAsync(_query); }
    }

    public int TypeFilterIndex
    {
        get => _typeFilterIndex;
        set { SetProperty(ref _typeFilterIndex, value); _ = SearchAsync(_query); }
    }

    public string MinSize
    {
        get => _minSize;
        set { SetProperty(ref _minSize, value); _ = SearchAsync(_query); }
    }

    public string MaxSize
    {
        get => _maxSize;
        set { SetProperty(ref _maxSize, value); _ = SearchAsync(_query); }
    }

    public bool ShowFilters
    {
        get => _showFilters;
        set => SetProperty(ref _showFilters, value);
    }

    // Localized strings
    public string SearchWatermark => Lang.T("SearchFiles");
    public string ResultsFormat => Lang.T("FoundResults");
    public string FiltersLabel => Lang.T("Filters");
    public string MatchCaseLabel => Lang.T("MatchCase");
    public string WholeWordLabel => Lang.T("WholeWord");
    public string MatchPathLabel => Lang.T("MatchPath");
    public string RegexLabel => Lang.T("Regex");
    public string TypeLabel => Lang.T("Type");
    public string MinSizeLabel => Lang.T("MinSize");
    public string MaxSizeLabel => Lang.T("MaxSize");

    public MainViewModel()
    {
        Results.CollectionChanged += (s, e) =>
            OnPropertyChanged(nameof(ResultCountText));
    }

    public MainViewModel(AnythingSearchService searchService) : this()
    {
        _searchService = searchService;
        LoadFilterSettings();
    }

    private void LoadFilterSettings()
    {
        var s = SettingsManager.Current;
        _matchCase = s.MatchCase;
        _matchWholeWord = s.MatchWholeWord;
        _matchPath = s.MatchPath;
        _useRegex = s.UseRegex;
        _typeFilterIndex = (int)s.TypeFilter;
        _minSize = s.MinSize?.ToString() ?? "";
        _maxSize = s.MaxSize?.ToString() ?? "";
    }

    private void SaveFilterSettings()
    {
        var s = SettingsManager.Current;
        s.MatchCase = _matchCase;
        s.MatchWholeWord = _matchWholeWord;
        s.MatchPath = _matchPath;
        s.UseRegex = _useRegex;
        s.TypeFilter = (FilterType)_typeFilterIndex;
        s.MinSize = long.TryParse(_minSize, out var min) ? min : null;
        s.MaxSize = long.TryParse(_maxSize, out var max) ? max : null;
        SettingsManager.Save();
    }

    public string ResultCountText =>
        string.Format(Lang.T("FoundResults"), Results.Count);

    public void ToggleFilters()
    {
        ShowFilters = !ShowFilters;
        OnPropertyChanged(nameof(ShowFilters));
    }

    public async Task InitializeAsync()
    {
        if (_searchService != null)
            await _searchService.BuildIndexAsync();
    }

    public async Task SetSearchServiceAsync(AnythingSearchService service)
    {
        _searchService = service;
        await _searchService.BuildIndexAsync();
    }

    private async Task SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            Results.Clear();
            OnPropertyChanged(nameof(ResultCountText));
            return;
        }

        if (_searchService == null)
            return;

        var options = new SearchOptions
        {
            MaxResults = SettingsManager.Current.MaxResults,
            MatchCase = _matchCase,
            MatchWholeWord = _matchWholeWord,
            MatchPath = _matchPath,
            UseRegex = _useRegex,
            TypeFilter = (FilterType)_typeFilterIndex,
            MinSize = long.TryParse(_minSize, out var min) ? min : null,
            MaxSize = long.TryParse(_maxSize, out var max) ? max : null,
        };

        var items = await _searchService.SearchAsync(query, options);
        Results.Clear();

        foreach (var item in items)
            Results.Add(new FileEntryViewModel(item));

        OnPropertyChanged(nameof(ResultCountText));
    }

    public void OpenFile(FileEntryViewModel? entry)
    {
        if (entry == null) return;

        try
        {
            if (OperatingSystem.IsWindows())
                System.Diagnostics.Process.Start("explorer", $"\"{entry.Path}\"");
            else if (OperatingSystem.IsLinux())
                System.Diagnostics.Process.Start("xdg-open", $"\"{entry.Path}\"");
            else if (OperatingSystem.IsMacOS())
                System.Diagnostics.Process.Start("open", $"\"{entry.Path}\"");
            else
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = entry.Path,
                    UseShellExecute = true
                });
        }
        catch { }
    }
}
