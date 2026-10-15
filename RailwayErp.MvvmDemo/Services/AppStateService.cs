using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Messages;
using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.Services;

/// <summary>
/// Сервіс стану застосунку (сесії). Реєструється в DI як Singleton, тому існує в одному
/// екземплярі весь час роботи процесу і доступний будь-якій ViewModel через конструктор.
/// </summary>
public interface IAppStateService
{
    string SessionId { get; }
    DateTimeOffset SessionStartedAt { get; }
    AppLifecycleState LifecycleState { get; }
    int StopCount { get; }
    int ResumeCount { get; }
    DateTimeOffset? LastStoppedAt { get; }
    DateTimeOffset? LastResumedAt { get; }
    DateTimeOffset? LastSavedAt { get; }

    /// <summary>Поточна сторінка Shell (наприклад «//route»), відновлюється при холодному старті.</summary>
    string? LastPage { get; set; }

    /// <summary>Останній переглянутий поїзд.</summary>
    int? LastOpenedTrainId { get; set; }

    /// <summary>Незбережена чернетка форми редагування маршруту.</summary>
    RouteDraft? RouteDraft { get; }

    void SetLifecycleState(AppLifecycleState state);

    void UpdateRouteDraft(RouteDraft draft);

    void ClearRouteDraft();

    /// <summary>Записує стан у Preferences (викликається при Stopped та Destroying).</summary>
    void SaveState();

    /// <summary>Читає стан із Preferences (викликається при старті процесу).</summary>
    void RestoreState();
}

public sealed class AppStateService(IPreferences preferences, ILogger<AppStateService> logger) : IAppStateService
{
    public const string LastPageKey = "last_page";
    private const string RouteDraftKey = "state.route_draft";
    private const string LastTrainKey = "state.last_train_id";
    private const string SavedAtKey = "state.saved_at";

    public string SessionId { get; } = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    public DateTimeOffset SessionStartedAt { get; } = DateTimeOffset.Now;
    public AppLifecycleState LifecycleState { get; private set; } = AppLifecycleState.NotStarted;
    public int StopCount { get; private set; }
    public int ResumeCount { get; private set; }
    public DateTimeOffset? LastStoppedAt { get; private set; }
    public DateTimeOffset? LastResumedAt { get; private set; }
    public DateTimeOffset? LastSavedAt { get; private set; }
    public string? LastPage { get; set; }
    public int? LastOpenedTrainId { get; set; }
    public RouteDraft? RouteDraft { get; private set; }

    public void SetLifecycleState(AppLifecycleState state)
    {
        LifecycleState = state;

        switch (state)
        {
            case AppLifecycleState.Stopped:
                StopCount++;
                LastStoppedAt = DateTimeOffset.Now;
                break;
            case AppLifecycleState.Resumed:
                ResumeCount++;
                LastResumedAt = DateTimeOffset.Now;
                break;
        }

        WeakReferenceMessenger.Default.Send(new AppLifecycleChangedMessage(state));
    }

    public void UpdateRouteDraft(RouteDraft draft) => RouteDraft = draft.IsEmpty ? null : draft;

    public void ClearRouteDraft()
    {
        RouteDraft = null;
        preferences.Remove(RouteDraftKey);
    }

    public void SaveState()
    {
        if (LastPage is not null)
            preferences.Set(LastPageKey, LastPage);

        if (LastOpenedTrainId is { } trainId)
            preferences.Set(LastTrainKey, trainId);

        if (RouteDraft is { } draft)
            preferences.Set(RouteDraftKey, JsonSerializer.Serialize(draft, AppStateJsonContext.Default.RouteDraft));
        else
            preferences.Remove(RouteDraftKey);

        LastSavedAt = DateTimeOffset.Now;
        preferences.Set(SavedAtKey, LastSavedAt.Value.ToString("O"));

        logger.LogInformation("Стан збережено в Preferences: сторінка={Page}, чернетка={Draft}",
            LastPage ?? "—", RouteDraft?.Title ?? "немає");
    }

    public void RestoreState()
    {
        LastPage = preferences.Get<string?>(LastPageKey, null);

        var trainId = preferences.Get(LastTrainKey, -1);
        LastOpenedTrainId = trainId >= 0 ? trainId : null;

        var json = preferences.Get<string?>(RouteDraftKey, null);
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                RouteDraft = JsonSerializer.Deserialize(json, AppStateJsonContext.Default.RouteDraft);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Пошкоджена чернетка в Preferences — її видалено");
                preferences.Remove(RouteDraftKey);
            }
        }

        var savedAt = preferences.Get<string?>(SavedAtKey, null);
        logger.LogInformation("Стан відновлено з Preferences (збережено {SavedAt}): сторінка={Page}, чернетка={Draft}",
            savedAt is null ? "ніколи" : DateTimeOffset.Parse(savedAt).ToString("dd.MM HH:mm:ss"),
            LastPage ?? "—", RouteDraft?.Title ?? "немає");
    }
}

/// <summary>Source-generated контекст System.Text.Json (без рефлексії — безпечно для trimming на Android).</summary>
[JsonSerializable(typeof(RouteDraft))]
internal sealed partial class AppStateJsonContext : JsonSerializerContext;
