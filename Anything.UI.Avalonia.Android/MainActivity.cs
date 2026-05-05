using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using Anything.UI.Avalonia;

namespace Anything.UI.Avalonia.Droid;

[Activity(Label = "Anything", MainLauncher = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return builder
            .UseAndroid()
            .WithInterFont()
            .UseSkia();
    }
}
