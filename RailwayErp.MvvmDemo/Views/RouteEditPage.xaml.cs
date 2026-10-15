using RailwayErp.MvvmDemo.ViewModels;

namespace RailwayErp.MvvmDemo.Views;

public partial class RouteEditPage : ContentPage
{
    private readonly RouteEditViewModel _viewModel;

    public RouteEditPage(RouteEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (Application.Current is { } app)
            app.RequestedThemeChanged += OnRequestedThemeChanged;

        // Тему могли змінити на іншій вкладці, поки сторінка була прихована.
        RefreshPickerColors();

        await _viewModel.OnAppearingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (Application.Current is { } app)
            app.RequestedThemeChanged -= OnRequestedThemeChanged;

        _viewModel.OnDisappearing();
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e) =>
        Dispatcher.Dispatch(RefreshPickerColors);

    /// <summary>
    /// Обхід особливості WinUI-обробника DatePicker/TimePicker: після зміни теми «на льоту»
    /// колір тексту дати лишається від попередньої теми. Повторно застосовуємо TextColor.
    /// </summary>
    private void RefreshPickerColors()
    {
#if WINDOWS
        // Нативний CalendarDatePicker не перечитує ThemeResource при зміні теми предка
        // (microsoft-ui-xaml#2698), тому явно задаємо йому поточну тему застосунку.
        var theme = Application.Current?.RequestedTheme == AppTheme.Dark
            ? Microsoft.UI.Xaml.ElementTheme.Dark
            : Microsoft.UI.Xaml.ElementTheme.Light;
        foreach (var picker in new View[] { DepartureDatePicker, DepartureTimePicker })
        {
            if (picker.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement element)
                element.RequestedTheme = theme;
        }
#endif
        DepartureDatePicker.Handler?.UpdateValue(nameof(DatePicker.TextColor));
        DepartureTimePicker.Handler?.UpdateValue(nameof(TimePicker.TextColor));
    }
}
