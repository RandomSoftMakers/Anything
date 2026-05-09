using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Anything.UI.Avalonia.ViewModels;

namespace Anything.UI.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        CustomTitleBar.IsVisible = true;
        SystemDecorations = SystemDecorations.BorderOnly;
        SetGlassBackground();
    }

    private void SetGlassBackground()
    {
        var theme = App.ResolveTheme(Settings.SettingsManager.Current.Theme);
        if (theme == "LiquidGlass")
        {
            Background = new SolidColorBrush(Color.Parse("#1A0A1E"));
        }
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void ToggleFilters_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.ToggleFilters();
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

    private void ListBox_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is FileEntryViewModel vm)
        {
            if (DataContext is MainViewModel mainVm)
                mainVm.OpenFile(vm);
        }
    }

    private void ListBox_Loaded(object? sender, RoutedEventArgs e) { }
}