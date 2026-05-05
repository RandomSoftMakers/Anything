using Anything.UI.Avalonia.Settings;

namespace Anything.UI.Avalonia.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private int _selectedThemeIndex;
    private int _selectedLanguageIndex;
    private bool _useNativeTitleBar;
    private string _maxResults = "500";

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

    public bool UseNativeTitleBar
    {
        get => _useNativeTitleBar;
        set => SetProperty(ref _useNativeTitleBar, value);
    }

    public string MaxResults
    {
        get => _maxResults;
        set => SetProperty(ref _maxResults, value);
    }

    public SettingsViewModel()
    {
        var settings = SettingsManager.Current;
        _selectedThemeIndex = settings.Theme == "Light" ? 1 : 0;
        _selectedLanguageIndex = settings.Language == "ru-RU" ? 1 : 0;
        _useNativeTitleBar = settings.UseNativeTitleBar;
        _maxResults = settings.MaxResults.ToString();
    }

    public void Save()
    {
        var settings = SettingsManager.Current;
        settings.Theme = SelectedThemeIndex == 0 ? "Dark" : "Light";
        settings.Language = SelectedLanguageIndex == 0 ? "en-US" : "ru-RU";
        settings.UseNativeTitleBar = UseNativeTitleBar;

        if (int.TryParse(MaxResults, out int maxResults))
        {
            settings.MaxResults = maxResults;
        }

        SettingsManager.Save();
        App.ApplyTheme(settings.Theme);
    }
}
