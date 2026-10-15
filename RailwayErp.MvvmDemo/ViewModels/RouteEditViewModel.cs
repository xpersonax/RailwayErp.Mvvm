using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Messages;
using RailwayErp.MvvmDemo.Models;
using RailwayErp.MvvmDemo.Services;

namespace RailwayErp.MvvmDemo.ViewModels;

/// <summary>
/// Форма редагування маршруту. Кожна зміна поля оновлює чернетку в IAppStateService (у пам'яті),
/// а при Window.Stopped App записує її в Preferences. Після повернення (Resumed) або після
/// перезапуску процесу форма відновлюється з чернетки.
/// </summary>
public sealed partial class RouteEditViewModel : BaseViewModel, IRecipient<AppLifecycleChangedMessage>
{
    private static readonly HashSet<string> FormProperties =
    [
        nameof(RouteNumber), nameof(SelectedTrain), nameof(DepartureStation), nameof(ArrivalStation),
        nameof(DepartureDate), nameof(DepartureTime), nameof(IntermediateStops), nameof(Notes), nameof(IsExpress)
    ];

    private readonly IAppStateService _appState;
    private readonly ITrainRepository _trains;
    private readonly IRouteRepository _routes;
    private readonly IDialogService _dialogs;
    private readonly ILogger<RouteEditViewModel> _logger;

    private bool _isApplyingDraft;
    private bool _isInitialized;

    public RouteEditViewModel(IAppStateService appState, ITrainRepository trains, IRouteRepository routes,
        IDialogService dialogs, ILogger<RouteEditViewModel> logger) : base("Редагування маршруту")
    {
        _appState = appState;
        _trains = trains;
        _routes = routes;
        _dialogs = dialogs;
        _logger = logger;

        // Початкові значення не повинні перезаписати чернетку, збережену в IAppStateService.
        _isApplyingDraft = true;
        RouteNumber = DepartureStation = ArrivalStation = IntermediateStops = Notes = DraftStatus = string.Empty;
        _isApplyingDraft = false;
        ResetForm();
    }

    public ObservableCollection<Train> AvailableTrains { get; } = [];

    [ObservableProperty]
    public partial string RouteNumber { get; set; }

    [ObservableProperty]
    public partial Train? SelectedTrain { get; set; }

    [ObservableProperty]
    public partial string DepartureStation { get; set; }

    [ObservableProperty]
    public partial string ArrivalStation { get; set; }

    [ObservableProperty]
    public partial DateTime? DepartureDate { get; set; }

    [ObservableProperty]
    public partial TimeSpan? DepartureTime { get; set; }

    [ObservableProperty]
    public partial string IntermediateStops { get; set; }

    [ObservableProperty]
    public partial string Notes { get; set; }

    [ObservableProperty]
    public partial bool IsExpress { get; set; }

    [ObservableProperty]
    public partial string? ValidationMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStateBanner))]
    public partial string? StateBanner { get; set; }

    [ObservableProperty]
    public partial string DraftStatus { get; set; }

    [ObservableProperty]
    public partial int SavedRoutesCount { get; set; }

    public bool HasStateBanner => !string.IsNullOrEmpty(StateBanner);

    public DateTime MinimumDate => DateTime.Today;

    /// <summary>Викликається з RouteEditPage.OnAppearing.</summary>
    public async Task OnAppearingAsync()
    {
        if (!WeakReferenceMessenger.Default.IsRegistered<AppLifecycleChangedMessage>(this))
            WeakReferenceMessenger.Default.Register(this);

        if (AvailableTrains.Count == 0)
        {
            foreach (var train in await _trains.GetAllAsync())
                AvailableTrains.Add(train);
        }

        if (!_isInitialized)
        {
            _isInitialized = true;
            if (_appState.RouteDraft is { } draft)
            {
                ApplyDraft(draft);
                StateBanner = $"Відновлено незбережену чернетку «{draft.Title}» (остання зміна {draft.UpdatedAt:dd.MM HH:mm:ss}).";
                _logger.LogInformation("Форму маршруту відновлено з сервісу стану: {Title}", draft.Title);
            }
        }

        SavedRoutesCount = _routes.SavedRoutes.Count;
    }

    /// <summary>Викликається з RouteEditPage.OnDisappearing.</summary>
    public void OnDisappearing()
    {
        WeakReferenceMessenger.Default.Unregister<AppLifecycleChangedMessage>(this);
        PushDraftToState();
    }

    public void Receive(AppLifecycleChangedMessage message)
    {
        switch (message.State)
        {
            case AppLifecycleState.Stopped:
                // App одразу після цього повідомлення викличе SaveState() — чернетка має бути актуальною.
                PushDraftToState();
                StateBanner = $"Застосунок згорнуто о {DateTime.Now:HH:mm:ss} — стан форми збережено (Window.Stopped).";
                break;

            case AppLifecycleState.Resumed when message.ReturnedFromBackground:
                if (_appState.RouteDraft is { } draft)
                {
                    ApplyDraft(draft);
                    StateBanner = $"Повернення о {DateTime.Now:HH:mm:ss} (Window.Resumed): форму відновлено зі стану, збереженого о {_appState.LastSavedAt:HH:mm:ss}.";
                    _logger.LogInformation("Форму маршруту відновлено після Resumed: {Title}", draft.Title);
                }
                else
                {
                    StateBanner = null;
                }
                break;
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (!_isApplyingDraft && e.PropertyName is { } name && FormProperties.Contains(name))
            PushDraftToState();
    }

    partial void OnSelectedTrainChanged(Train? value)
    {
        if (_isApplyingDraft || value is null)
            return;

        // Зручність: підставляємо станції обраного поїзда, якщо поля ще порожні.
        if (string.IsNullOrWhiteSpace(DepartureStation))
            DepartureStation = value.From;
        if (string.IsNullOrWhiteSpace(ArrivalStation))
            ArrivalStation = value.To;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationMessage = Validate();
        if (ValidationMessage is not null)
            return;

        var route = BuildDraft();
        IsBusy = true;
        try
        {
            await _routes.SaveAsync(route);
        }
        finally
        {
            IsBusy = false;
        }

        _appState.ClearRouteDraft();
        ResetForm();
        StateBanner = null;
        SavedRoutesCount = _routes.SavedRoutes.Count;
        _logger.LogInformation("Маршрут збережено: {Title}; чернетку очищено", route.Title);

        await _dialogs.ShowAlertAsync("Маршрут збережено",
            $"{route.Title}\n{route.DepartureDate:dd.MM.yyyy} о {route.DepartureTime:hh\\:mm}");
    }

    [RelayCommand]
    private void Clear()
    {
        ResetForm();
        _appState.ClearRouteDraft();
        StateBanner = null;
        ValidationMessage = null;
        _logger.LogInformation("Форму маршруту очищено, чернетку видалено");
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(RouteNumber))
            return "Вкажіть номер маршруту.";
        if (string.IsNullOrWhiteSpace(DepartureStation) || string.IsNullOrWhiteSpace(ArrivalStation))
            return "Вкажіть станції відправлення та прибуття.";
        if (string.Equals(DepartureStation.Trim(), ArrivalStation.Trim(), StringComparison.CurrentCultureIgnoreCase))
            return "Станції відправлення та прибуття мають відрізнятися.";
        if (DepartureDate is { } date && date.Date < DateTime.Today)
            return "Дата відправлення не може бути в минулому.";
        return null;
    }

    private RouteDraft BuildDraft() => new()
    {
        RouteNumber = RouteNumber.Trim(),
        TrainId = SelectedTrain?.Id,
        DepartureStation = DepartureStation.Trim(),
        ArrivalStation = ArrivalStation.Trim(),
        DepartureDate = DepartureDate ?? DateTime.Today,
        DepartureTime = DepartureTime ?? TimeSpan.Zero,
        IntermediateStops = IntermediateStops,
        Notes = Notes,
        IsExpress = IsExpress,
        UpdatedAt = DateTimeOffset.Now
    };

    private void PushDraftToState()
    {
        var draft = BuildDraft();
        _appState.UpdateRouteDraft(draft);
        DraftStatus = draft.IsEmpty
            ? "Чернетка порожня"
            : $"Чернетку оновлено в IAppStateService о {draft.UpdatedAt:HH:mm:ss}";
    }

    private void ApplyDraft(RouteDraft draft)
    {
        _isApplyingDraft = true;
        try
        {
            RouteNumber = draft.RouteNumber;
            SelectedTrain = AvailableTrains.FirstOrDefault(t => t.Id == draft.TrainId);
            DepartureStation = draft.DepartureStation;
            ArrivalStation = draft.ArrivalStation;
            DepartureDate = draft.DepartureDate;
            DepartureTime = draft.DepartureTime;
            IntermediateStops = draft.IntermediateStops;
            Notes = draft.Notes;
            IsExpress = draft.IsExpress;
        }
        finally
        {
            _isApplyingDraft = false;
        }

        DraftStatus = $"Чернетку відновлено (остання зміна {draft.UpdatedAt:HH:mm:ss})";
    }

    private void ResetForm()
    {
        _isApplyingDraft = true;
        try
        {
            RouteNumber = DepartureStation = ArrivalStation = IntermediateStops = Notes = string.Empty;
            SelectedTrain = null;
            DepartureDate = DateTime.Today;
            DepartureTime = new TimeSpan(8, 0, 0);
            IsExpress = false;
        }
        finally
        {
            _isApplyingDraft = false;
        }

        DraftStatus = "Чернетка порожня";
    }
}
