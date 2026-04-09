using Microsoft.Win32;
using System;
using System.Windows;

namespace Anything.UI.Wpf;

public static class ThemeManager
{
    public static string CurrentTheme { get; private set; } = "Dark";

    public static void Initialize()
    {
        try
        {
            CurrentTheme = DetectWindowsTheme();
            ApplyTheme(CurrentTheme);

            SystemEvents.UserPreferenceChanged += (_, e) =>
            {
                if (e.Category == UserPreferenceCategory.General)
                {
                    string newTheme = DetectWindowsTheme();
                    if (newTheme != CurrentTheme)
                    {
                        CurrentTheme = newTheme;
                        ApplyTheme(CurrentTheme);
                    }
                }
            };
        }
        catch
        {
            ApplyTheme("Dark");
        }
    }

    public static string DetectWindowsTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            if (key?.GetValue("AppsUseLightTheme") is int v)
                return v == 1 ? "Light" : "Dark";
        }
        catch { }

        return "Dark";
    }

    public static void ApplyTheme(string theme)
    {
        try
        {
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(
                new ResourceDictionary { Source = new Uri($"Themes/{theme}.xaml", UriKind.Relative) });
        }
        catch { }
    }
}
