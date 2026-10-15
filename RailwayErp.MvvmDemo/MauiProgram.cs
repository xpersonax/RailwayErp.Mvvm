using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using RailwayErp.MvvmDemo.Services;
using RailwayErp.MvvmDemo.Services.Logging;
using RailwayErp.MvvmDemo.ViewModels;
using RailwayErp.MvvmDemo.Views;

namespace RailwayErp.MvvmDemo;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureLifecycleEvents(ConfigurePlatformLifecycle);

        // --- Логування: Debug Output + сторінка «Журнал» + файл lifecycle.log ---
        var logStore = new LogStore();
        builder.Services.AddSingleton<ILogStore>(logStore);
        builder.Logging.AddProvider(new LogStoreLoggerProvider(logStore));
#if DEBUG
        builder.Logging.AddDebug();
#endif

        // --- Сервіси платформи (MAUI Essentials) ---
        builder.Services.AddSingleton(Preferences.Default);

        // --- Сервіси застосунку. Singleton: один екземпляр на весь час роботи процесу ---
        builder.Services.AddSingleton<IAppStateService, AppStateService>();
        builder.Services.AddSingleton<IThemeService, ThemeService>();
        builder.Services.AddSingleton<IDeepLinkService, DeepLinkService>();
        builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
        builder.Services.AddSingleton<IDialogService, ShellDialogService>();
        builder.Services.AddSingleton<ITrainRepository, InMemoryTrainRepository>();
        builder.Services.AddSingleton<IRouteRepository, InMemoryRouteRepository>();
#if WINDOWS
        builder.Services.AddSingleton<IProtocolRegistrationService, WinUI.WindowsProtocolRegistrationService>();
#else
        builder.Services.AddSingleton<IProtocolRegistrationService, DeclarativeProtocolRegistrationService>();
#endif

        // --- Shell, ViewModel'і та сторінки. Transient: новий екземпляр при кожному запиті ---
        builder.Services.AddTransient<AppShell>();

        builder.Services.AddTransient<TrainListViewModel>();
        builder.Services.AddTransient<TrainListPage>();

        builder.Services.AddTransient<TrainDetailsViewModel>();
        builder.Services.AddTransient<TrainDetailsPage>();

        builder.Services.AddTransient<RouteEditViewModel>();
        builder.Services.AddTransient<RouteEditPage>();

        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<SettingsPage>();

        builder.Services.AddTransient<LogViewModel>();
        builder.Services.AddTransient<LogPage>();

        return builder.Build();
    }

    /// <summary>
    /// Нативні події життєвого циклу платформи. Події Window у App — це крос-платформна
    /// абстракція над ними (напр. Android OnStop / WinUI VisibilityChanged(false) → Window.Stopped).
    /// </summary>
    private static void ConfigurePlatformLifecycle(ILifecycleBuilder events)
    {
#if ANDROID
        events.AddAndroid(android => android
            .OnCreate((_, _) => LogPlatform("Android: Activity.OnCreate"))
            .OnStart(_ => LogPlatform("Android: Activity.OnStart"))
            .OnResume(_ => LogPlatform("Android: Activity.OnResume"))
            .OnPause(_ => LogPlatform("Android: Activity.OnPause"))
            .OnStop(_ => LogPlatform("Android: Activity.OnStop"))
            .OnDestroy(_ => LogPlatform("Android: Activity.OnDestroy")));
#elif WINDOWS
        events.AddWindows(windows => windows
            .OnLaunched((_, _) =>
            {
                LogPlatform("WinUI: Application.OnLaunched");

                // Deep link у Windows приходить як protocol-активація або аргумент командного рядка.
                if (WinUI.WindowsLaunchArguments.GetDeepLink() is { } uri)
                    Microsoft.Maui.Controls.Application.Current?.SendOnAppLinkRequestReceived(uri);
            })
            .OnVisibilityChanged((_, args) => LogPlatform($"WinUI: Window.VisibilityChanged (Visible={args.Visible})"))
            .OnClosed((_, _) => LogPlatform("WinUI: Window.Closed")));
#endif
    }

    private static void LogPlatform(string message) =>
        IPlatformApplication.Current?.Services.GetService<ILoggerFactory>()
            ?.CreateLogger("RailwayErp.MvvmDemo.Platform")
            .LogInformation("{PlatformEvent}", message);
}
