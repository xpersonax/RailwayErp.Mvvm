using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RailwayErp.MvvmDemo.Services;

namespace RailwayErp.MvvmDemo.ViewModels;

/// <summary>Налаштування: тема оформлення та перевірка deep link.</summary>
public sealed partial class SettingsViewModel : BaseViewModel
{
    private readonly IThemeService _theme;
    private readonly IDeepLinkService _deepLinks;
    private readonly IProtocolRegistrationService _protocol;

    public SettingsViewModel(IThemeService theme, IDeepLinkService deepLinks, IProtocolRegistrationService protocol)
        : base("Налаштування")
    {
        _theme = theme;
        _deepLinks = deepLinks;
        _protocol = protocol;

        SelectedTheme = theme.SelectedTheme;
        ThemeStatus = string.Empty;
        DeepLinkInput = DeepLinks.Examples[0];
        UpdateThemeStatus();
    }

    public IReadOnlyList<string> DeepLinkExamples => DeepLinks.Examples;

    public bool IsProtocolRegistrationSupported => _protocol.IsSupported;

    /// <summary>Прив'язано до RadioButtonGroup.SelectedValue на сторінці.</summary>
    [ObservableProperty]
    public partial AppTheme SelectedTheme { get; set; }

    [ObservableProperty]
    public partial string ThemeStatus { get; set; }

    [ObservableProperty]
    public partial string DeepLinkInput { get; set; }

    [ObservableProperty]
    public partial string? DeepLinkResult { get; set; }

    [ObservableProperty]
    public partial string? ProtocolStatus { get; set; }

    partial void OnSelectedThemeChanged(AppTheme value)
    {
        _theme.SetTheme(value);
        UpdateThemeStatus();
    }

    public void OnAppearing()
    {
        SelectedTheme = _theme.SelectedTheme;
        UpdateThemeStatus();
    }

    [RelayCommand]
    private void UseExample(string example) => DeepLinkInput = example;

    [RelayCommand]
    private async Task OpenDeepLinkAsync()
    {
        if (!Uri.TryCreate(DeepLinkInput?.Trim(), UriKind.Absolute, out var uri))
        {
            DeepLinkResult = "Некоректний URI.";
            return;
        }

        DeepLinkResult = _deepLinks.TryResolve(uri, out var target)
            ? $"→ {target.Description} ({target.Route})"
            : "Посилання не підтримується застосунком.";

        await _deepLinks.HandleAsync(uri);
    }

    [RelayCommand]
    private void RegisterProtocol() =>
        ProtocolStatus = _protocol.Register()
            ? $"Схему {DeepLinks.Scheme}:// зареєстровано для поточного користувача. Перевірка: Win+R → {DeepLinks.Examples[0]}"
            : "Не вдалося зареєструвати схему (див. журнал).";

    [RelayCommand]
    private void UnregisterProtocol() =>
        ProtocolStatus = _protocol.Unregister()
            ? $"Реєстрацію схеми {DeepLinks.Scheme}:// скасовано."
            : "Не вдалося скасувати реєстрацію (див. журнал).";

    private void UpdateThemeStatus() =>
        ThemeStatus = $"Обрано: {ThemeService.Describe(_theme.SelectedTheme)} · застосовано: {ThemeService.Describe(_theme.EffectiveTheme)} · Preferences[\"{ThemeService.ThemeKey}\"]";
}
