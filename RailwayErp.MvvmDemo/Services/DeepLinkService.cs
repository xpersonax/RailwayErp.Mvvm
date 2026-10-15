using System.Diagnostics.CodeAnalysis;
using System.Web;
using Microsoft.Extensions.Logging;

namespace RailwayErp.MvvmDemo.Services;

public static class DeepLinks
{
    /// <summary>Власна URI-схема застосунку: railwayerp://train/3</summary>
    public const string Scheme = "railwayerp";

    /// <summary>Домен для Android App Links: https://railwayerp.example.com/train/3</summary>
    public const string WebHost = "railwayerp.example.com";

    public static readonly string[] Examples =
    [
        "railwayerp://train/3",
        "railwayerp://trains/7",
        "railwayerp://train?id=2",
        "https://railwayerp.example.com/train/5",
        "railwayerp://route",
        "railwayerp://settings"
    ];
}

/// <summary>Результат розбору посилання: маршрут Shell + параметри навігації.</summary>
public sealed record DeepLinkTarget(string Route, IDictionary<string, object>? Parameters, string Description);

public interface IDeepLinkService
{
    bool HasPendingLink { get; }

    /// <summary>Перетворює URI на маршрут Shell. Чиста функція — не навігує.</summary>
    bool TryResolve(Uri uri, [NotNullWhen(true)] out DeepLinkTarget? target);

    /// <summary>Обробляє посилання: навігує або відкладає, якщо Shell ще не готовий.</summary>
    Task<bool> HandleAsync(Uri uri);

    /// <summary>Викликається, коли Shell завантажено: обробляє відкладене посилання.</summary>
    Task OnNavigationReadyAsync();
}

public sealed class DeepLinkService(INavigationService navigation, ILogger<DeepLinkService> logger) : IDeepLinkService
{
    private Uri? _pendingLink;
    private bool _isNavigationReady;

    public bool HasPendingLink => _pendingLink is not null;

    public bool TryResolve(Uri uri, [NotNullWhen(true)] out DeepLinkTarget? target)
    {
        target = null;
        if (!uri.IsAbsoluteUri)
            return false;

        var isCustomScheme = uri.Scheme.Equals(DeepLinks.Scheme, StringComparison.OrdinalIgnoreCase);
        var isAppLink = (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) &&
                        uri.Host.Equals(DeepLinks.WebHost, StringComparison.OrdinalIgnoreCase);
        if (!isCustomScheme && !isAppLink)
            return false;

        // railwayerp://train/3 → Host = "train", Path = "/3";  https://host/train/3 → Path = "/train/3"
        var segments = new List<string>();
        if (isCustomScheme && !string.IsNullOrEmpty(uri.Host))
            segments.Add(uri.Host);
        segments.AddRange(uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries));
        segments = segments.Select(s => Uri.UnescapeDataString(s).ToLowerInvariant()).ToList();

        var query = HttpUtility.ParseQueryString(uri.Query);
        var section = segments.FirstOrDefault() ?? "trains";

        switch (section)
        {
            case "train" or "trains":
                var idText = segments.ElementAtOrDefault(1) ?? query["id"];
                if (idText is null)
                {
                    target = new DeepLinkTarget("//trains", null, "Список поїздів");
                    return true;
                }

                if (!int.TryParse(idText, out var id) || id <= 0)
                    return false;

                target = new DeepLinkTarget("//trains/traindetails", new Dictionary<string, object>
                {
                    ["id"] = id,
                    ["source"] = "deeplink",
                    ["link"] = uri.OriginalString
                }, $"Деталі поїзда #{id}");
                return true;

            case "route" or "routes":
                target = new DeepLinkTarget("//route", null, "Редагування маршруту");
                return true;

            case "log" or "journal":
                target = new DeepLinkTarget("//log", null, "Журнал");
                return true;

            case "settings":
                target = new DeepLinkTarget("//settings", null, "Налаштування");
                return true;

            default:
                return false;
        }
    }

    public async Task<bool> HandleAsync(Uri uri)
    {
        if (!TryResolve(uri, out var target))
        {
            logger.LogWarning("Deep link не розпізнано: {Uri}", uri.OriginalString);
            return false;
        }

        if (!_isNavigationReady)
        {
            _pendingLink = uri;
            logger.LogInformation("Deep link {Uri} отримано до готовності Shell — відкладено", uri.OriginalString);
            return true;
        }

        logger.LogInformation("Deep link {Uri} → {Route} ({Description})", uri.OriginalString, target.Route, target.Description);
        await MainThread.InvokeOnMainThreadAsync(() => navigation.GoToAsync(target.Route, target.Parameters));
        return true;
    }

    public async Task OnNavigationReadyAsync()
    {
        _isNavigationReady = true;

        if (_pendingLink is { } uri)
        {
            _pendingLink = null;
            await HandleAsync(uri);
        }
    }
}
