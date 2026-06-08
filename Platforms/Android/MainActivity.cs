using Android.App;
using Android.Content.PM;
using Android.OS;

using AndroidColor = Android.Graphics.Color;

namespace MobileSLI;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize
                           | ConfigChanges.Orientation
                           | ConfigChanges.UiMode
                           | ConfigChanges.ScreenLayout
                           | ConfigChanges.SmallestScreenSize
                           | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

#pragma warning disable CA1422
        Window?.SetStatusBarColor(AndroidColor.ParseColor("#0F172A"));
        Window?.SetNavigationBarColor(AndroidColor.ParseColor("#0F172A"));
#pragma warning restore CA1422
    }
}
