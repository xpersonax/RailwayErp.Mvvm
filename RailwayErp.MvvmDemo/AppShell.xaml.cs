using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Services;
using RailwayErp.MvvmDemo.Views;

namespace RailwayErp.MvvmDemo;

public partial class AppShell : Shell
{
    private readonly IAppStateService _appState;
    private readonly ILogger<AppShell> _logger;

    public AppShell(IAppStateService appState, ILogger<AppShell> logger)
    {
        InitializeComponent();
        _appState = appState;
        _logger = logger;

        // Маршрут сторінки, що не є вкладкою: //trains/traindetails?id=3
        Routing.RegisterRoute("traindetails", typeof(TrainDetailsPage));
    }

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);

        // Поточна сторінка — частина стану сесії; при Stopped/Destroying вона потрапить у Preferences.
        _appState.LastPage = args.Current?.Location.OriginalString;
        _logger.LogInformation("Shell.Navigated: {Previous} → {Current} ({Source})",
            args.Previous?.Location.OriginalString ?? "—", args.Current?.Location.OriginalString ?? "—", args.Source);
    }
}
