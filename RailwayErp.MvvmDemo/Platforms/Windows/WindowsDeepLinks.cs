using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppLifecycle;
using RailwayErp.MvvmDemo.Services;
using Windows.ApplicationModel.Activation;

namespace RailwayErp.MvvmDemo.WinUI;

/// <summary>Отримання deep link, з яким запущено процес у Windows.</summary>
public static class WindowsLaunchArguments
{
    // Так Windows App SDK передає URI unpackaged-застосунку при protocol-активації.
    private const string ProtocolArgumentPrefix = "----ms-protocol:";

    public static Uri? GetDeepLink()
    {
        try
        {
            var activation = AppInstance.GetCurrent().GetActivatedEventArgs();
            if (activation.Kind == ExtendedActivationKind.Protocol && activation.Data is IProtocolActivatedEventArgs protocol)
                return protocol.Uri;
        }
        catch (Exception)
        {
            // AppLifecycle недоступний — переходимо до аргументів командного рядка.
        }

        foreach (var argument in Environment.GetCommandLineArgs().Skip(1))
        {
            var candidate = argument.StartsWith(ProtocolArgumentPrefix, StringComparison.OrdinalIgnoreCase)
                ? argument[ProtocolArgumentPrefix.Length..]
                : argument;

            if (Uri.TryCreate(candidate.Trim('"'), UriKind.Absolute, out var uri) &&
                (uri.Scheme.Equals(DeepLinks.Scheme, StringComparison.OrdinalIgnoreCase) ||
                 uri.Host.Equals(DeepLinks.WebHost, StringComparison.OrdinalIgnoreCase)))
            {
                return uri;
            }
        }

        return null;
    }
}

/// <summary>
/// Реєстрація railwayerp:// для unpackaged WinUI-застосунку (HKCU, для поточного користувача).
/// Для packaged (MSIX) варіанта ту саму схему оголошено в Package.appxmanifest.
/// </summary>
public sealed class WindowsProtocolRegistrationService(ILogger<WindowsProtocolRegistrationService> logger)
    : IProtocolRegistrationService
{
    public bool IsSupported => true;

    public bool Register()
    {
        try
        {
            var exePath = Environment.ProcessPath!;
            ActivationRegistrationManager.RegisterForProtocolActivation(DeepLinks.Scheme, $"{exePath},0", "Railway ERP", exePath);
            logger.LogInformation("Схему {Scheme}:// зареєстровано в Windows для {Exe}", DeepLinks.Scheme, exePath);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не вдалося зареєструвати схему {Scheme}://", DeepLinks.Scheme);
            return false;
        }
    }

    public bool Unregister()
    {
        try
        {
            ActivationRegistrationManager.UnregisterForProtocolActivation(DeepLinks.Scheme, Environment.ProcessPath!);
            logger.LogInformation("Реєстрацію схеми {Scheme}:// скасовано", DeepLinks.Scheme);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не вдалося скасувати реєстрацію схеми {Scheme}://", DeepLinks.Scheme);
            return false;
        }
    }
}
