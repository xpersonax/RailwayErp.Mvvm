using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Messages;
using RailwayErp.MvvmDemo.Models;
using RailwayErp.MvvmDemo.Services;

namespace RailwayErp.MvvmDemo.ViewModels;

/// <summary>
/// Список поїздів. Дані завантажуються командою LoadCommand, яку викликає сторінка в OnAppearing.
/// Поки сторінка видима і застосунок на передньому плані, працює фонове автооновлення статусів
/// (PeriodicTimer у Task.Run); воно зупиняється в OnDisappearing та при Window.Stopped.
/// </summary>
public sealed partial class TrainListViewModel : BaseViewModel, IRecipient<AppLifecycleChangedMessage>
{
    private static readonly TimeSpan AutoRefreshInterval = TimeSpan.FromSeconds(15);

    private readonly ITrainRepository _repository;
    private readonly INavigationService _navigation;
    private readonly ILogger<TrainListViewModel> _logger;
    private CancellationTokenSource? _autoRefreshCts;

    public TrainListViewModel(ITrainRepository repository, INavigationService navigation, ILogger<TrainListViewModel> logger)
        : base("Поїзди")
    {
        _repository = repository;
        _navigation = navigation;
        _logger = logger;
        LastUpdatedText = "Дані ще не завантажено";
        AutoRefreshText = "Автооновлення вимкнено";
    }

    public ObservableCollection<Train> Trains { get; } = [];

    [ObservableProperty]
    public partial string LastUpdatedText { get; set; }

    [ObservableProperty]
    public partial string AutoRefreshText { get; set; }

    [ObservableProperty]
    public partial bool IsAutoRefreshRunning { get; set; }

    /// <summary>Стан RefreshView (pull-to-refresh), окремо від IsBusy.</summary>
    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            var trains = await _repository.GetAllAsync();

            Trains.Clear();
            foreach (var train in trains)
                Trains.Add(train);

            LastUpdatedText = $"Оновлено о {DateTime.Now:HH:mm:ss} · {Trains.Count} поїздів";
            _logger.LogInformation("LoadCommand: завантажено {Count} поїздів", Trains.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не вдалося завантажити список поїздів");
            LastUpdatedText = "Помилка завантаження";
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private Task OpenTrainAsync(Train? train) =>
        train is null ? Task.CompletedTask : _navigation.GoToAsync($"traindetails?id={train.Id}");

    /// <summary>Викликається з TrainListPage.OnAppearing.</summary>
    public void Activate()
    {
        if (!WeakReferenceMessenger.Default.IsRegistered<AppLifecycleChangedMessage>(this))
            WeakReferenceMessenger.Default.Register(this);

        StartAutoRefresh();
    }

    /// <summary>Викликається з TrainListPage.OnDisappearing: звільняємо ресурси сторінки.</summary>
    public void Deactivate()
    {
        WeakReferenceMessenger.Default.Unregister<AppLifecycleChangedMessage>(this);
        StopAutoRefresh("сторінку приховано");
    }

    public void Receive(AppLifecycleChangedMessage message)
    {
        switch (message.State)
        {
            case AppLifecycleState.Stopped:
                StopAutoRefresh("застосунок згорнуто");
                break;

            case AppLifecycleState.Resumed when message.ReturnedFromBackground:
                StartAutoRefresh();
                LoadCommand.Execute(null); // після повернення показуємо свіжі дані
                break;
        }
    }

    private void StartAutoRefresh()
    {
        if (_autoRefreshCts is not null)
            return;

        _autoRefreshCts = new CancellationTokenSource();
        var token = _autoRefreshCts.Token;
        _ = Task.Run(() => AutoRefreshLoopAsync(token), token);

        IsAutoRefreshRunning = true;
        AutoRefreshText = $"Автооновлення статусів кожні {AutoRefreshInterval.TotalSeconds:0} с";
        _logger.LogInformation("Фонове автооновлення запущено (кожні {Seconds} с)", AutoRefreshInterval.TotalSeconds);
    }

    private void StopAutoRefresh(string reason)
    {
        if (_autoRefreshCts is null)
            return;

        _autoRefreshCts.Cancel();
        _autoRefreshCts.Dispose();
        _autoRefreshCts = null;

        IsAutoRefreshRunning = false;
        AutoRefreshText = $"Автооновлення призупинено: {reason}";
        _logger.LogInformation("Фонове автооновлення зупинено: {Reason}", reason);
    }

    private async Task AutoRefreshLoopAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(AutoRefreshInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                await _repository.RefreshLiveStatusAsync(token);
                await MainThread.InvokeOnMainThreadAsync(() => LoadCommand.ExecuteAsync(null));
            }
        }
        catch (OperationCanceledException)
        {
            // Нормальне завершення: сторінка зникла або застосунок пішов у фон.
        }
    }
}
