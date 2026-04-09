using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Anything.Core.Services;
using Anything.Platform.Windows;
using Anything.Core.Models;

namespace Anything.UI.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Icon = new BitmapImage(new Uri("pack://application:,,,/Assets/icon.png"));

        var provider = new WindowsFileIndexProvider();
        var service = new AnythingSearchService(provider);
        DataContext = new MainViewModel(service);

        Loaded += async (_, _) =>
        {
            ApplyMicaOrFallback();
            await ((MainViewModel)DataContext).InitializeAsync();
        };
    }

    private void OpenAbout_Click(object sender, RoutedEventArgs e)
    {
        new AboutWindow { Owner = this }.ShowDialog();
    }

    private void ListView_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.ListView lv &&
            lv.SelectedItem is FileEntry entry)
        {
            try
            {
                Process.Start(new ProcessStartInfo(entry.Path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Cannot open file");
            }
        }
    }

    private void ApplyMicaOrFallback()
    {
        try
        {
            if (Environment.OSVersion.Version.Build < 22000)
                return;

            var hwnd = new WindowInteropHelper(this).EnsureHandle();
            TryEnableMica(hwnd);
        }
        catch { }
    }

    private void TryEnableMica(IntPtr hwnd)
    {
        const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        int mica = 2;
        int dark = ThemeManager.CurrentTheme == "Dark" ? 1 : 0;

        try
        {
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref mica, sizeof(int));
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
        }
        catch { }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attr,
        ref int attrValue,
        int attrSize);
}
