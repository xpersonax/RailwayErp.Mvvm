using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.Services;

public interface IRouteRepository
{
    IReadOnlyList<RouteDraft> SavedRoutes { get; }

    Task SaveAsync(RouteDraft route, CancellationToken cancellationToken = default);
}

public sealed class InMemoryRouteRepository : IRouteRepository
{
    private readonly List<RouteDraft> _routes = [];

    public IReadOnlyList<RouteDraft> SavedRoutes => _routes;

    public async Task SaveAsync(RouteDraft route, CancellationToken cancellationToken = default)
    {
        await Task.Delay(300, cancellationToken);
        _routes.Add(route);
    }
}
