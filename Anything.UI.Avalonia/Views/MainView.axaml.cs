using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Anything.UI.Avalonia.ViewModels;

namespace Anything.UI.Avalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void Settings_Click(object? sender, RoutedEventArgs e)
    {
    }

    private void ListBox_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is FileEntryViewModel vm)
        {
            if (DataContext is MainViewModel mainVm)
                mainVm.OpenFile(vm);
        }
    }
}