using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anything.UI.Avalonia.Settings;

public class AppSettings
{
    public bool IsFirstRun { get; set; } = true;
    public string Theme { get; set; } = "Dark";
    public string Language { get; set; } = "en-US";
    public bool UseNativeTitleBar { get; set; } = false;
    public int MaxResults { get; set; } = 500;
    public bool StartMinimized { get; set; } = false;
    public string HotKey { get; set; } = "Alt+Space";
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
