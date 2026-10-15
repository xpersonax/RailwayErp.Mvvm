using Microsoft.Extensions.Logging;

namespace RailwayErp.MvvmDemo.Services;

/// <summary>Перемикання світлої/темної теми з persistence через Preferences.</summary>
public interface IThemeService
{
    /// <summary>Вибір користувача: Light, Dark або Unspecified (як у системі).</summary>
    AppTheme SelectedTheme { get; }

    /// <summary>Тема, яка фактично застосована зараз (з урахуванням системної).</summary>
    AppTheme EffectiveTheme { get; }

    void ApplySavedTheme();

    void SetTheme(AppTheme theme);
}

public sealed class ThemeService(IPreferences preferences, ILogger<ThemeService> logger) : IThemeService
{
    public const string ThemeKey = "app_theme";

    public AppTheme SelectedTheme { get; private set; } = AppTheme.Unspecified;

    public AppTheme EffectiveTheme => Application.Current?.RequestedTheme ?? AppTheme.Unspecified;

    public void ApplySavedTheme()
    {
        var saved = (AppTheme)preferences.Get(ThemeKey, (int)AppTheme.Unspecified);
        if (!Enum.IsDefined(saved))
            saved = AppTheme.Unspecified;

        Apply(saved);
        logger.LogInformation("Тему відновлено з Preferences: {Theme}", Describe(saved));
    }

    public void SetTheme(AppTheme theme)
    {
        if (theme == SelectedTheme)
            return;

        Apply(theme);
        preferences.Set(ThemeKey, (int)theme);
        logger.LogInformation("Тему змінено на «{Theme}» і збережено в Preferences[{Key}]", Describe(theme), ThemeKey);
    }

    public static string Describe(AppTheme theme) => theme switch
    {
        AppTheme.Light => "світла",
        AppTheme.Dark => "темна",
        _ => "як у системі"
    };

    private void Apply(AppTheme theme)
    {
        SelectedTheme = theme;

        // UserAppTheme діє на весь застосунок: усі AppThemeBinding на всіх сторінках
        // перераховуються автоматично, тому окремо оновлювати сторінки не потрібно.
        if (Application.Current is { } app)
            app.UserAppTheme = theme;
    }
}
