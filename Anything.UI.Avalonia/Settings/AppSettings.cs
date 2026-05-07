using System;
using System.IO;
using System.Text.Json;
using Anything.Core.Models;

namespace Anything.UI.Avalonia.Settings;

public class AppSettings
{
    public bool IsFirstRun { get; set; } = true;
    public string Theme { get; set; } = "Dark";
    public string Language { get; set; } = "en-US";
    public int MaxResults { get; set; } = 500;
    public bool StartMinimized { get; set; } = false;
    public string HotKey { get; set; } = "Alt+Space";

    public bool MatchCase { get; set; } = false;
    public bool MatchWholeWord { get; set; } = false;
    public bool MatchPath { get; set; } = false;
    public bool UseRegex { get; set; } = false;
    public FilterType TypeFilter { get; set; } = FilterType.All;
    public long? MinSize { get; set; }
    public long? MaxSize { get; set; }
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    public string SearchLocation { get; set; } = "";

    public bool EnableIndexer { get; set; } = true;
    public bool IndexerAutoStart { get; set; } = true;
    public string IndexPath { get; set; } = "";

    public static string LanguageCode(int index) => index switch
    {
        0 => "en-US", 1 => "ru-RU", 2 => "de-DE", 3 => "fr-FR",
        4 => "es-ES", 5 => "zh-CN", 6 => "ja-JP", _ => "en-US"
    };

    public static int LanguageIndex(string code) => code switch
    {
        "en-US" => 0, "ru-RU" => 1, "de-DE" => 2, "fr-FR" => 3,
        "es-ES" => 4, "zh-CN" => 5, "ja-JP" => 6, _ => 0
    };

    public static string ThemeName(int index) => App.ThemeNames[index];
    public static int ThemeIndex(string name) => Array.IndexOf(App.ThemeNames, name);
}

public static class SettingsManager
{
    private static readonly string SettingsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "Anything", "settings.json");

    private static AppSettings? _settings;

    public static AppSettings Current
    {
        get
        {
            if (_settings == null)
            {
                Load();
            }
            return _settings!;
        }
    }

    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                return;
            }
        }
        catch
        {
            // Ignore and use defaults
        }

        _settings = new AppSettings();
    }

    public static void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath)!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}
