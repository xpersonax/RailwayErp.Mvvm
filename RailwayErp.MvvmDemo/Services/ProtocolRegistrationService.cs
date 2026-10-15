namespace RailwayErp.MvvmDemo.Services;

/// <summary>
/// Реєстрація URI-схеми railwayerp:// в ОС. На Android схема оголошується статично
/// (IntentFilter у MainActivity), на Windows для unpackaged-застосунку — у рантаймі.
/// </summary>
public interface IProtocolRegistrationService
{
    bool IsSupported { get; }

    bool Register();

    bool Unregister();
}

/// <summary>Реалізація за замовчуванням для платформ, де реєстрація декларативна.</summary>
public sealed class DeclarativeProtocolRegistrationService : IProtocolRegistrationService
{
    public bool IsSupported => false;

    public bool Register() => false;

    public bool Unregister() => false;
}
