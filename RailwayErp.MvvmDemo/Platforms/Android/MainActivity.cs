using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace RailwayErp.MvvmDemo;

// LaunchMode.SingleTop: якщо застосунок уже відкритий, нове посилання прийде в OnNewIntent,
// а не створить другу копію Activity.
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, Exported = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
// Deep link із власною схемою: railwayerp://train/3
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "railwayerp")]
// Android App Link (https). AutoVerify вимагає файл /.well-known/assetlinks.json на домені.
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "https",
    DataHost = "railwayerp.example.com",
    DataPathPrefix = "/train",
    AutoVerify = true)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleDeepLink(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        HandleDeepLink(intent);
    }

    private static void HandleDeepLink(Intent? intent)
    {
        if (intent?.Action != Intent.ActionView || string.IsNullOrEmpty(intent.DataString))
            return;

        if (Uri.TryCreate(intent.DataString, UriKind.Absolute, out var uri))
            Microsoft.Maui.Controls.Application.Current?.SendOnAppLinkRequestReceived(uri);
    }
}
