using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Anything.Core.Models;
using Anything.Core.Services;
using Anything.Core.Abstractions;
using Anything.UI.Avalonia.Settings;

namespace Anything.UI.Avalonia.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly AnythingSearchService? _searchService;
    private string _query = string.Empty;
    private bool _useNativeTitleBar;

    public ObservableCollection<FileEntryViewModel> Results { get; } = new();
    public int ResultCount => Results.Count;

    public string Query
    {
        get => _query;
        set
        {
            try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "anything-search.log"), $"Query changed to: {value}\n"); } catch { }
            if (SetProperty(ref _query, value))
            {
                try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "anything-search.log"), $"Property set, calling SearchAsync with: {_query}\n"); } catch { }
                _ = SearchAsync(_query);
            }
        }
    }

    public bool UseNativeTitleBar
    {
        get => _useNativeTitleBar;
        set => SetProperty(ref _useNativeTitleBar, value);
    }

    public MainViewModel()
    {
        Results.CollectionChanged += (s, e) =>
        {
            try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "anything-search.log"), $"Results collection changed! Count: {Results.Count}\n"); } catch { }
        };
        try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "anything-search.log"), "MainViewModel default constructor called\n"); } catch { }
    }

    public MainViewModel(AnythingSearchService searchService)
    {
        Results.CollectionChanged += (s, e) =>
        {
            try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "anything-search.log"), $"Results collection changed! Count: {Results.Count}\n"); } catch { }
        };
        _searchService = searchService;
        _useNativeTitleBar = SettingsManager.Current.UseNativeTitleBar;

        try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "anything-search.log"), "MainViewModel with service created\n"); } catch { }
        _ = TestSearchAsync();
    }

    private async Task TestSearchAsync()
    {
        await Task.Delay(5000); // Wait for UI to load
        try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "anything-search.log"), "Running test search for 'test'...\n"); } catch { }
        Query = "test";
        try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "anything-search.log"), $"Query set to 'test', Results count: {Results.Count}\n"); } catch { }
    }

    public async Task InitializeAsync()
    {
        System.Diagnostics.Debug.WriteLine("Starting index build...");
        if (_searchService != null)
        {
            await _searchService.BuildIndexAsync();
            System.Diagnostics.Debug.WriteLine("Index build completed.");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Warning: _searchService is null!");
        }
    }

    private async Task SearchAsync(string query)
    {
        System.Diagnostics.Debug.WriteLine($"Searching for: {query}");
        if (string.IsNullOrWhiteSpace(query))
        {
            Results.Clear();
            OnPropertyChanged(nameof(Results));
            return;
        }

        if (_searchService == null)
        {
            System.Diagnostics.Debug.WriteLine("Warning: _searchService is null in SearchAsync!");
            return;
        }

        var items = await _searchService.SearchAsync(query);
        Results.Clear();
        System.Diagnostics.Debug.WriteLine($"Found {items.Count()} items");

        foreach (var item in items.Take(SettingsManager.Current.MaxResults))
        {
            Results.Add(new FileEntryViewModel(item));
        }
        OnPropertyChanged(nameof(Results));
        try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "anything-search.log"), $"After adding, Results.Count = {Results.Count}\n"); } catch { }
    }

    public void OpenFile(FileEntryViewModel? entry)
    {
        if (entry == null)
            return;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                System.Diagnostics.Process.Start("explorer", $"\"{entry.Path}\"");
            }
            else if (OperatingSystem.IsLinux())
            {
                System.Diagnostics.Process.Start("xdg-open", $"\"{entry.Path}\"");
            }
            else if (OperatingSystem.IsMacOS())
            {
                System.Diagnostics.Process.Start("open", $"\"{entry.Path}\"");
            }
            else
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = entry.Path,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Ignore errors
        }
    }
}
