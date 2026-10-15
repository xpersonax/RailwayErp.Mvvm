using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.Services;

public interface ITrainRepository
{
    Task<IReadOnlyList<Train>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Train?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Імітує надходження оперативних даних від диспетчерської (затримки, посадка).</summary>
    Task RefreshLiveStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>Демонстраційний репозиторій у пам'яті з затримкою, що імітує мережу/БД.</summary>
public sealed class InMemoryTrainRepository : ITrainRepository
{
    private static readonly TimeSpan Latency = TimeSpan.FromMilliseconds(350);

    private readonly object _lock = new();
    private readonly Random _random = new(2026);
    private List<Train> _trains =
    [
        new() { Id = 1, Number = "091К", From = "Київ", To = "Львів", Departure = new(6, 25, 0), Arrival = new(11, 41, 0), Category = "Інтерсіті+", Locomotive = "HRCS2-008", Wagons = 9, Status = TrainStatus.OnTime },
        new() { Id = 2, Number = "105К", From = "Київ", To = "Одеса", Departure = new(7, 4, 0), Arrival = new(14, 10, 0), Category = "Інтерсіті", Locomotive = "ЧС8-034", Wagons = 12, Status = TrainStatus.Boarding },
        new() { Id = 3, Number = "763К", From = "Харків", To = "Київ", Departure = new(8, 15, 0), Arrival = new(13, 2, 0), Category = "Інтерсіті+", Locomotive = "HRCS2-004", Wagons = 9, Status = TrainStatus.Delayed, DelayMinutes = 12 },
        new() { Id = 4, Number = "079П", From = "Дніпро", To = "Львів", Departure = new(9, 40, 0), Arrival = new(23, 55, 0), Category = "Пасажирський", Locomotive = "ЧС4-095", Wagons = 15, Status = TrainStatus.OnTime },
        new() { Id = 5, Number = "715К", From = "Київ", To = "Перемишль", Departure = new(10, 55, 0), Arrival = new(18, 5, 0), Category = "Інтерсіті+", Locomotive = "HRCS2-011", Wagons = 9, Status = TrainStatus.OnTime },
        new() { Id = 6, Number = "082П", From = "Ужгород", To = "Київ", Departure = new(14, 20, 0), Arrival = new(6, 30, 0), Category = "Нічний експрес", Locomotive = "ЧС7-221", Wagons = 17, Status = TrainStatus.Departed },
        new() { Id = 7, Number = "047Д", From = "Запоріжжя", To = "Київ", Departure = new(19, 12, 0), Arrival = new(5, 48, 0), Category = "Нічний експрес", Locomotive = "ЧС8-012", Wagons = 16, Status = TrainStatus.Cancelled },
        new() { Id = 8, Number = "143Л", From = "Київ", To = "Ворохта", Departure = new(21, 30, 0), Arrival = new(10, 45, 0), Category = "Пасажирський", Locomotive = "ВЛ11-187", Wagons = 14, Status = TrainStatus.OnTime }
    ];

    public async Task<IReadOnlyList<Train>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(Latency, cancellationToken);
        lock (_lock)
        {
            return _trains.OrderBy(t => t.Departure).ToList();
        }
    }

    public async Task<Train?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await Task.Delay(Latency / 2, cancellationToken);
        lock (_lock)
        {
            return _trains.FirstOrDefault(t => t.Id == id);
        }
    }

    public Task RefreshLiveStatusAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _trains = _trains.Select(train => train.Status is TrainStatus.Cancelled or TrainStatus.Departed
                ? train
                : _random.Next(4) switch
                {
                    0 => train.With(TrainStatus.Delayed, _random.Next(3, 25)),
                    1 => train.With(TrainStatus.Boarding, 0),
                    _ => train.With(TrainStatus.OnTime, 0)
                }).ToList();
        }

        return Task.CompletedTask;
    }
}
