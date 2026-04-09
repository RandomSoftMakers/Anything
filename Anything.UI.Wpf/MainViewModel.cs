using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Anything.Core.Models;
using Anything.Core.Services;

namespace Anything.UI.Wpf;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly AnythingSearchService _searchService;
    private string _query = string.Empty;

    public ObservableCollection<FileEntry> Results { get; } = new();

    public string Query
    {
        get => _query;
        set
        {
            if (SetField(ref _query, value))
            {
                _ = SearchAsync(_query);
            }
        }
    }

    public MainViewModel(AnythingSearchService searchService)
    {
        _searchService = searchService;
    }

    public async Task InitializeAsync()
    {
        // Построение индекса (может занять время)
        await _searchService.BuildIndexAsync();
    }

    private async Task SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            Results.Clear();
            return;
        }

        var items = await _searchService.SearchAsync(query);
        Results.Clear();

        foreach (var item in items.Take(500))
            Results.Add(item);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
