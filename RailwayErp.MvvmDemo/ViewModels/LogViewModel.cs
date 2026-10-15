using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RailwayErp.MvvmDemo.Messages;
using RailwayErp.MvvmDemo.Models;
using RailwayErp.MvvmDemo.Services;
using RailwayErp.MvvmDemo.Services.Logging;

namespace RailwayErp.MvvmDemo.ViewModels;

/// <summary>Журнал подій життєвого циклу + картка поточної сесії з IAppStateService.</summary>
public sealed partial class LogViewModel : BaseViewModel, IRecipient<AppLifecycleChangedMessage>
{
    private readonly ILogStore _store;
    private readonly IAppStateService _appState;

    public LogViewModel(ILogStore store, IAppStateService appState) : base("Журнал")
    {
        _store = store;
        _appState = appState;
        SessionText = StateText = CountersText = SavedText = string.Empty;
        RefreshSession();
    }

    public ObservableCollection<LogEntry> Entries => _store.Entries;

    public string LogFilePath => _store.LogFilePath;

    [ObservableProperty]
    public partial string SessionText { get; set; }

    [ObservableProperty]
    public partial string StateText { get; set; }

    [ObservableProperty]
    public partial string CountersText { get; set; }

    [ObservableProperty]
    public partial string SavedText { get; set; }

    public void OnAppearing()
    {
        if (!WeakReferenceMessenger.Default.IsRegistered<AppLifecycleChangedMessage>(this))
            WeakReferenceMessenger.Default.Register(this);
        RefreshSession();
    }

    public void OnDisappearing() => WeakReferenceMessenger.Default.Unregister<AppLifecycleChangedMessage>(this);

    public void Receive(AppLifecycleChangedMessage message) => RefreshSession();

    [RelayCommand]
    private void Clear() => _store.Clear();

    private void RefreshSession()
    {
        SessionText = $"Сесія {_appState.SessionId} · старт {_appState.SessionStartedAt:HH:mm:ss}";
        StateText = $"Поточний стан: {_appState.LifecycleState}";
        CountersText = $"Згортань (Stopped): {_appState.StopCount} · відновлень (Resumed): {_appState.ResumeCount}";
        SavedText = _appState.LastSavedAt is { } saved
            ? $"Останнє збереження стану: {saved:HH:mm:ss} · сторінка {_appState.LastPage ?? "—"}"
            : "Стан у цій сесії ще не зберігався";
    }
}
