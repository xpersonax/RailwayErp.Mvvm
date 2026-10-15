namespace RailwayErp.MvvmDemo.Services;

/// <summary>
/// Абстракція над Shell-навігацією, щоб ViewModel'і не залежали від Shell.Current напряму.
/// </summary>
public interface INavigationService
{
    string? CurrentLocation { get; }

    Task GoToAsync(string route, IDictionary<string, object>? parameters = null);

    Task GoBackAsync();
}

public sealed class ShellNavigationService : INavigationService
{
    public string? CurrentLocation => Shell.Current?.CurrentState?.Location?.OriginalString;

    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        if (Shell.Current is not { } shell)
            return Task.CompletedTask;

        return parameters is null ? shell.GoToAsync(route) : shell.GoToAsync(route, parameters);
    }

    public Task GoBackAsync() => Shell.Current?.GoToAsync("..") ?? Task.CompletedTask;
}

/// <summary>Діалоги (alert) без прямого звернення ViewModel'ей до Page.</summary>
public interface IDialogService
{
    Task ShowAlertAsync(string title, string message);
}

public sealed class ShellDialogService : IDialogService
{
    public Task ShowAlertAsync(string title, string message) =>
        Shell.Current?.DisplayAlertAsync(title, message, "OK") ?? Task.CompletedTask;
}
