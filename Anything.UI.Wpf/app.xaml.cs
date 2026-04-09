using System;
using System.Windows;

namespace Anything.UI.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
        {
            MessageBox.Show(ex.ExceptionObject.ToString(), "Fatal Error");
        };

        DispatcherUnhandledException += (_, ex) =>
        {
            MessageBox.Show(ex.Exception.Message, "UI Error");
            ex.Handled = true;
        };

        ThemeManager.Initialize();
    }
}
