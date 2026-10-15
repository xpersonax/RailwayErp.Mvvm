namespace RailwayErp.MvvmDemo.Models;

/// <summary>
/// Знімок форми редагування маршруту. Саме цей об'єкт серіалізується в Preferences,
/// коли застосунок згортається (Window.Stopped), і читається під час наступного запуску.
/// </summary>
public sealed class RouteDraft
{
    public string RouteNumber { get; set; } = string.Empty;
    public int? TrainId { get; set; }
    public string DepartureStation { get; set; } = string.Empty;
    public string ArrivalStation { get; set; } = string.Empty;
    public DateTime DepartureDate { get; set; } = DateTime.Today;
    public TimeSpan DepartureTime { get; set; } = new(8, 0, 0);
    public string IntermediateStops { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsExpress { get; set; }

    /// <summary>Коли чернетку востаннє змінено у формі.</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(RouteNumber) &&
        TrainId is null &&
        string.IsNullOrWhiteSpace(DepartureStation) &&
        string.IsNullOrWhiteSpace(ArrivalStation) &&
        string.IsNullOrWhiteSpace(IntermediateStops) &&
        string.IsNullOrWhiteSpace(Notes) &&
        !IsExpress;

    public string Title => string.IsNullOrWhiteSpace(RouteNumber)
        ? $"{DepartureStation} — {ArrivalStation}"
        : $"№{RouteNumber}: {DepartureStation} — {ArrivalStation}";
}
