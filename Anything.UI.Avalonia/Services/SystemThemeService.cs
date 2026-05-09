using System.Diagnostics;
using System.Runtime.Versioning;

namespace Anything.UI.Avalonia.Services;

public static class SystemThemeService
{
    public static bool IsSystemDarkMode()
    {
        try
        {
            if (OperatingSystem.IsWindows())
                return IsWindowsDarkMode();
            else if (OperatingSystem.IsLinux())
                return IsLinuxDarkMode();
        }
        catch
        {
        }

        return false;
    }

    [SupportedOSPlatform("windows")]
    private static bool IsWindowsDarkMode()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int val)
                return val == 0;
        }
        catch
        {
        }

        return false;
    }

    private static bool IsLinuxDarkMode()
    {
        var dark = TryDetectGnome();
        if (dark.HasValue) return dark.Value;

        dark = TryDetectKde();
        if (dark.HasValue) return dark.Value;

        dark = TryDetectGtk();
        if (dark.HasValue) return dark.Value;

        return false;
    }

    private static bool? TryDetectGnome()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "gsettings",
                Arguments = "get org.gnome.desktop.interface color-scheme",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process == null) return null;
            var output = process.StandardOutput.ReadToEnd()?.Trim().Trim('\'');
            process.WaitForExit(2000);
            if (process.ExitCode != 0) return null;

            if (string.IsNullOrEmpty(output)) return null;

            if (output.Contains("dark", StringComparison.OrdinalIgnoreCase))
                return true;
            if (output == "default" || output.Contains("light", StringComparison.OrdinalIgnoreCase))
                return false;

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool? TryDetectKde()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "kreadconfig5",
                Arguments = "--group General --key ColorScheme",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process == null) return null;
            var output = process.StandardOutput.ReadToEnd()?.Trim();
            process.WaitForExit(2000);
            if (process.ExitCode != 0) return null;

            if (string.IsNullOrEmpty(output)) return null;

            return output.Contains("Dark", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return null;
        }
    }

    private static bool? TryDetectGtk()
    {
        try
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(home)) return null;

            var paths = new[]
            {
                Path.Combine(home, ".config", "gtk-3.0", "settings.ini"),
                Path.Combine(home, ".config", "gtk-4.0", "settings.ini"),
            };

            foreach (var path in paths)
            {
                if (!File.Exists(path)) continue;

                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("gtk-application-prefer-dark-theme=", StringComparison.OrdinalIgnoreCase))
                    {
                        var val = trimmed.Split('=')[1].Trim();
                        if (val == "1") return true;
                        if (val == "0") return false;
                    }
                }
            }
        }
        catch
        {
        }

        return null;
    }
}
