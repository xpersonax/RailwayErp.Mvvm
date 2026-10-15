namespace RailwayErp.MvvmDemo.Models;

/// <summary>Оперативний статус поїзда.</summary>
public enum TrainStatus
{
    OnTime,
    Boarding,
    Delayed,
    Departed,
    Cancelled
}

/// <summary>Поїзд у розкладі залізничної компанії.</summary>
public sealed class Train
{
    public required int Id { get; init; }
    public required string Number { get; init; }
    public required string From { get; init; }
    public required string To { get; init; }
    public required TimeSpan Departure { get; init; }
    public required TimeSpan Arrival { get; init; }
    public required string Category { get; init; }
    public required string Locomotive { get; init; }
    public int Wagons { get; init; }
    public int DelayMinutes { get; init; }
    public TrainStatus Status { get; init; }

    public string Name => $"{From} — {To}";

    public string DisplayName => $"{Number} {Name}";

    public string ScheduleText => $"{Departure:hh\\:mm} → {Arrival:hh\\:mm}";

    public string DepartureText => $"{From}, {Departure:hh\\:mm}";

    public string ArrivalText => $"{To}, {Arrival:hh\\:mm}";

    public string StatusText => Status switch
    {
        TrainStatus.OnTime => "За розкладом",
        TrainStatus.Boarding => "Посадка",
        TrainStatus.Delayed => $"Затримка {DelayMinutes} хв",
        TrainStatus.Departed => "Відправлено",
        TrainStatus.Cancelled => "Скасовано",
        _ => Status.ToString()
    };

    public Train With(TrainStatus status, int delayMinutes) => new()
    {
        Id = Id,
        Number = Number,
        From = From,
        To = To,
        Departure = Departure,
        Arrival = Arrival,
        Category = Category,
        Locomotive = Locomotive,
        Wagons = Wagons,
        Status = status,
        DelayMinutes = delayMinutes
    };
}
