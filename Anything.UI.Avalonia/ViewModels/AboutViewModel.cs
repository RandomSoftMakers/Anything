using System;
using System.Reflection;

namespace Anything.UI.Avalonia.ViewModels;

public class AboutViewModel : ViewModelBase
{
    public string AppName => "Anything";
    public string Version => "1.0.0";
    public string Description => "Lightning-fast cross-platform file search";
    public string Author => "Anything Development Team";
    public string License => "GPL-3.0";
    public string GitHubUrl => "https://github.com/AnythingDevelopmentTeam/Anything";
    public string DotNetVersion => Environment.Version.ToString();
    public string OsVersion => Environment.OSVersion.ToString();
    public string BuildDate
    {
        get
        {
            try
            {
                var entry = Assembly.GetEntryAssembly()?.Location;
                if (entry != null)
                    return File.GetLastWriteTimeUtc(entry).ToString("yyyy-MM-dd HH:mm:ss UTC");
            }
            catch { }
            return "Unknown";
        }
    }
}