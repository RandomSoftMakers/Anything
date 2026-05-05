using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Anything.UI.Avalonia.ViewModels;
using Anything.UI.Avalonia.Settings;

namespace Anything.UI.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ConfigureTitleBar();
    }

    private static bool IsTilingWindowManager()
    {
        // Check for Hyprland
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HYPRLAND_INSTANCE_SIGNATURE")))
            return true;

        // Check for Sway
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SWAYSOCK")))
            return true;

        // Check for i3
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("I3SOCK")))
            return true;

        return false;
    }

    private void ConfigureTitleBar()
    {
        bool useNative = SettingsManager.Current.UseNativeTitleBar;
        bool isTilingWM = IsTilingWindowManager();

        if (useNative && !isTilingWM)
        {
            ExtendClientAreaToDecorationsHint = false;
            ExtendClientAreaTitleBarHeightHint = 0;
            CustomTitleBar.IsVisible = false;
        }
        else
        {
            // For tiling WMs or when using custom titlebar, don't extend into decorations
            // Tiling WMs don't have decorations to extend into
            ExtendClientAreaToDecorationsHint = false;
            ExtendClientAreaTitleBarHeightHint = 0;
            CustomTitleBar.IsVisible = true;
        }

        System.Diagnostics.Debug.WriteLine($"ConfigureTitleBar: UseNativeTitleBar={useNative}, IsTilingWM={isTilingWM}, IsVisible={CustomTitleBar.IsVisible}");
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void Minimize_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

        private void Settings_Click(object? sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog(this);
        }

        private void TestSearch_Click(object? sender, RoutedEventArgs e)
        {
            File.AppendAllText("/tmp/anything-search.log", "Test button clicked\n");
            if (DataContext is MainViewModel vm)
            {
                File.AppendAllText("/tmp/anything-search.log", "DataContext is MainViewModel, setting Query to 'test'\n");
                vm.Query = "test";
            }
            else
            {
                File.AppendAllText("/tmp/anything-search.log", $"DataContext is NOT MainViewModel: {DataContext?.GetType().Name}\n");
            }
        }

        private void ListBox_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is FileEntryViewModel vm)
            {
                if (DataContext is MainViewModel mainVm)
                {
                    mainVm.OpenFile(vm);
                }
            }
        }

        private void ListBox_Loaded(object? sender, RoutedEventArgs e)
        {
            File.AppendAllText("/tmp/anything-ui.log", "ListBox loaded!\n");
            if (sender is ListBox listBox)
            {
                File.AppendAllText("/tmp/anything-ui.log", $"ListBox.Items.Count = {listBox.Items.Count}\n");
            }
        }
}
