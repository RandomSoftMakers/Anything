using System.Windows;
using System.Windows.Controls;

namespace Anything.UI.Wpf;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ThemeSelector_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;

        string theme = (ThemeSelector.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Dark";
        ThemeManager.ApplyTheme(theme);
    }
}
