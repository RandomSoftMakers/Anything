using Avalonia.Controls;
using Avalonia.Interactivity;
using Anything.UI.Avalonia.ViewModels;

namespace Anything.UI.Avalonia.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        DataContext = new AboutViewModel();
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void GitHubLink_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var url = "https://github.com/AnythingDevelopmentTeam/Anything";
            if (OperatingSystem.IsWindows())
                System.Diagnostics.Process.Start("explorer", url);
            else if (OperatingSystem.IsLinux())
                System.Diagnostics.Process.Start("xdg-open", url);
            else if (OperatingSystem.IsMacOS())
                System.Diagnostics.Process.Start("open", url);
        }
        catch { }
    }
}