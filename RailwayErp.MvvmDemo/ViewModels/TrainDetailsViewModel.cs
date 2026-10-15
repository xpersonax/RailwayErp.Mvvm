using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Models;
using RailwayErp.MvvmDemo.Services;

namespace RailwayErp.MvvmDemo.ViewModels;

/// <summary>
/// Деталі поїзда. Параметри приходять через IQueryAttributable як зі списку
/// (traindetails?id=3 — рядок), так і з deep link (словник: id — int, source, link).
/// </summary>
public sealed partial class TrainDetailsViewModel(
    ITrainRepository repository,
    IAppStateService appState,
    INavigationService navigation,
    ILogger<TrainDetailsViewModel> logger) : BaseViewModel("Деталі поїзда"), IQueryAttributable
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTrain))]
    public partial Train? Train { get; set; }

    [ObservableProperty]
    public partial bool IsNotFound { get; set; }

    [ObservableProperty]
    public partial int RequestedId { get; set; }

    [ObservableProperty]
    public partial bool IsOpenedFromDeepLink { get; set; }

    [ObservableProperty]
    public partial string? DeepLink { get; set; }

    public bool HasTrain => Train is not null;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var id = query.TryGetValue("id", out var rawId)
            ? rawId switch
            {
                int value => value,
                string text when int.TryParse(text, out var parsed) => parsed,
                _ => 0
            }
            : 0;

        IsOpenedFromDeepLink = query.TryGetValue("source", out var source) && source as string == "deeplink";
        DeepLink = IsOpenedFromDeepLink && query.TryGetValue("link", out var link) ? link as string : null;

        _ = LoadAsync(id);
    }

    private async Task LoadAsync(int id)
    {
        RequestedId = id;
        IsBusy = true;
        try
        {
            Train = await repository.GetByIdAsync(id);
            IsNotFound = Train is null;
            Title = Train is null ? "Поїзд не знайдено" : $"Поїзд {Train.Number}";

            if (Train is not null)
                appState.LastOpenedTrainId = id;

            logger.LogInformation("Відкрито деталі поїзда #{Id} ({Number}), джерело: {Source}",
                id, Train?.Number ?? "не знайдено", IsOpenedFromDeepLink ? $"deep link {DeepLink}" : "список");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task GoToListAsync() => navigation.GoToAsync("//trains");
}
