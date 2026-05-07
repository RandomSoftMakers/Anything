using Anything.UI.Avalonia.Settings;

namespace Anything.UI.Avalonia.ViewModels;

public class FirstRunViewModel : ViewModelBase
{
    private int _selectedThemeIndex = 0;
    private int _selectedLanguageIndex = 0;

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

    public void CompleteSetup()
    {
        var settings = SettingsManager.Current;
        settings.IsFirstRun = false;
        settings.Theme = AppSettings.ThemeName(SelectedThemeIndex);
        settings.Language = AppSettings.LanguageCode(SelectedLanguageIndex);
        SettingsManager.Save();

        App.ApplyTheme(settings.Theme);
    }

    public void Skip()
    {
        var settings = SettingsManager.Current;
        settings.IsFirstRun = false;
        SettingsManager.Save();
    }
}
