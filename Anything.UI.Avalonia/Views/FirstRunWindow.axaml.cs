using Avalonia.Controls;
using Avalonia.Interactivity;
using Anything.UI.Avalonia.ViewModels;

namespace Anything.UI.Avalonia.Views;

public partial class FirstRunWindow : Window
{
    public FirstRunWindow()
    {
        InitializeComponent();
        DataContext = new FirstRunViewModel();
    }

    private void Skip_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is FirstRunViewModel vm)
        {
            vm.Skip();
        }
        Close();
    }

    private void GetStarted_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is FirstRunViewModel vm)
        {
            vm.CompleteSetup();
        }
        Close();
    }
}
