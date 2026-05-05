using Anything.UI.Avalonia.Settings;

namespace Anything.UI.Avalonia.ViewModels;

public class FirstRunViewModel : ViewModelBase
{
    private int _selectedThemeIndex = 0;
    private int _selectedLanguageIndex = 0;
    private bool _useNativeTitleBar = false;

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

    public void CompleteSetup()
    {
        var settings = SettingsManager.Current;
        settings.IsFirstRun = false;
        settings.Theme = SelectedThemeIndex == 0 ? "Dark" : "Light";
        settings.Language = SelectedLanguageIndex == 0 ? "en-US" : "ru-RU";
        settings.UseNativeTitleBar = UseNativeTitleBar;
        SettingsManager.Save();

        // Apply theme immediately
        App.ApplyTheme(settings.Theme);
    }

    public void Skip()
    {
        var settings = SettingsManager.Current;
        settings.IsFirstRun = false;
        SettingsManager.Save();
    }
}
