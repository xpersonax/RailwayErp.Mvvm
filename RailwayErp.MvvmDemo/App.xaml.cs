using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Models;
using RailwayErp.MvvmDemo.Services;

namespace RailwayErp.MvvmDemo;

/// <summary>
/// Точка входу застосунку. Тут обробляються події життєвого циклу вікна
/// (Created / Activated / Deactivated / Stopped / Resumed / Destroying),
/// зберігається і відновлюється стан, застосовується тема та приймаються deep link.
/// </summary>
public partial class App : Application
{
    private readonly IServiceProvider _services;
    private readonly IAppStateService _appState;
    private readonly IThemeService _theme;
    private readonly IDeepLinkService _deepLinks;
    private readonly INavigationService _navigation;
    private readonly ILogger<App> _logger;

    public App(IServiceProvider services, IAppStateService appState, IThemeService theme,
        IDeepLinkService deepLinks, INavigationService navigation, ILogger<App> logger)
    {
        InitializeComponent();

        _services = services;
        _appState = appState;
        _theme = theme;
        _deepLinks = deepLinks;
        _navigation = navigation;
        _logger = logger;

        _logger.LogInformation("===== Новий процес: App створено (холодний старт) =====");

        // Якщо попередній процес було знищено у фоні, тут повертаємо чернетку та останню сторінку.
        _appState.RestoreState();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Тему застосовуємо до створення вікна — усі сторінки одразу відмалюються в обраній темі.
        _theme.ApplySavedTheme();

        var shell = _services.GetRequiredService<AppShell>();
        shell.Loaded += OnShellLoaded;

        var window = new Window(shell) { Title = "Railway ERP · MVVM Demo" };

        window.Created += OnWindowCreated;
        window.Activated += OnWindowActivated;
        window.Deactivated += OnWindowDeactivated;
        window.Stopped += OnWindowStopped;
        window.Resumed += OnWindowResumed;
        window.Destroying += OnWindowDestroying;

        return window;
    }

    /// <summary>Посилання ззовні (Android Intent, Windows protocol/аргументи командного рядка).</summary>
    protected override async void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);
        _logger.LogInformation("App.OnAppLinkRequestReceived: {Uri}", uri.OriginalString);
        await _deepLinks.HandleAsync(uri);
    }

    private void OnWindowCreated(object? sender, EventArgs e)
    {
        _appState.SetLifecycleState(AppLifecycleState.Created);
        _logger.LogInformation("Window.Created — вікно створено, ресурси ініціалізовано");
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        _appState.SetLifecycleState(AppLifecycleState.Activated);
        _logger.LogInformation("Window.Activated — застосунок на передньому плані та у фокусі");
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        _appState.SetLifecycleState(AppLifecycleState.Deactivated);
        _logger.LogInformation("Window.Deactivated — застосунок втратив фокус");
    }

    private void OnWindowStopped(object? sender, EventArgs e)
    {
        _logger.LogInformation("Window.Stopped — застосунок згорнуто у фон, зберігаємо стан");

        _appState.LastPage = _navigation.CurrentLocation ?? _appState.LastPage;

        // Спочатку сповіщаємо ViewModel'і (форма віддає актуальну чернетку, таймери зупиняються),
        // потім записуємо все в Preferences — процес у фоні ОС може знищити без попередження.
        _appState.SetLifecycleState(AppLifecycleState.Stopped);
        _appState.SaveState();
    }

    private void OnWindowResumed(object? sender, EventArgs e)
    {
        var inBackground = _appState.LastStoppedAt is { } stoppedAt
            ? (DateTimeOffset.Now - stoppedAt).TotalSeconds
            : 0;
        _logger.LogInformation("Window.Resumed — повернення на передній план (у фоні {Seconds:0.#} с), відновлюємо стан",
            inBackground);

        _appState.SetLifecycleState(AppLifecycleState.Resumed);
    }

    private void OnWindowDestroying(object? sender, EventArgs e)
    {
        _appState.LastPage = _navigation.CurrentLocation ?? _appState.LastPage;
        _appState.SetLifecycleState(AppLifecycleState.Destroying);
        _appState.SaveState();

        if (sender is Window window)
        {
            window.Created -= OnWindowCreated;
            window.Activated -= OnWindowActivated;
            window.Deactivated -= OnWindowDeactivated;
            window.Stopped -= OnWindowStopped;
            window.Resumed -= OnWindowResumed;
            window.Destroying -= OnWindowDestroying;
        }

        _logger.LogInformation("Window.Destroying — завершення роботи: стан збережено, обробники подій вікна відписано");
    }

    /// <summary>Shell готовий до навігації: відновлюємо останню сторінку або відкриваємо відкладений deep link.</summary>
    private async void OnShellLoaded(object? sender, EventArgs e)
    {
        if (sender is Shell shell)
            shell.Loaded -= OnShellLoaded;

        try
        {
            if (!_deepLinks.HasPendingLink)
                await RestoreLastPageAsync();

            await _deepLinks.OnNavigationReadyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка початкової навігації");
        }
    }

    private async Task RestoreLastPageAsync()
    {
        var lastPage = _appState.LastPage;
        if (string.IsNullOrEmpty(lastPage))
            return;

        var route = lastPage.Contains("traindetails", StringComparison.Ordinal) && _appState.LastOpenedTrainId is { } trainId
            ? $"//trains/traindetails?id={trainId}"
            : "//" + lastPage.TrimStart('/').Split('/', '?')[0];

        if (route == "//trains")
            return;

        _logger.LogInformation("Відновлення останньої сторінки з Preferences[\"{Key}\"]: {Route}", AppStateService.LastPageKey, route);
        await _navigation.GoToAsync(route);
    }
}
