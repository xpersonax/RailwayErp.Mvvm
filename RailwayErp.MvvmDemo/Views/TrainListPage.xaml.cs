using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.ViewModels;

namespace RailwayErp.MvvmDemo.Views;

public partial class TrainListPage : ContentPage
{
    private readonly TrainListViewModel _viewModel;
    private readonly ILogger<TrainListPage> _logger;

    // Сторінка та ViewModel створюються DI-контейнером (обидві зареєстровані як Transient).
    public TrainListPage(TrainListViewModel viewModel, ILogger<TrainListPage> logger)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _logger = logger;
    }

    /// <summary>Сторінка стає видимою — завантажуємо дані через команду ViewModel.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _logger.LogInformation("TrainListPage.OnAppearing → LoadCommand");

        _viewModel.Activate();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    /// <summary>Сторінка приховується — зупиняємо таймер автооновлення і логуємо перехід.</summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Deactivate();
        _logger.LogInformation("TrainListPage.OnDisappearing: перехід зі сторінки списку");
    }

    /// <summary>Shell-навігація: на сторінку перейшли (викликається після OnAppearing).</summary>
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        _logger.LogInformation("TrainListPage.OnNavigatedTo (тип: {Type}, звідки: {Previous})",
            args.NavigationType, args.PreviousPage?.GetType().Name ?? "—");
    }

    /// <summary>Shell-навігація: зі сторінки пішли — тут відомо, куди саме.</summary>
    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        _logger.LogInformation("TrainListPage.OnNavigatedFrom → {Destination} (тип: {Type})",
            args.DestinationPage?.GetType().Name ?? "—", args.NavigationType);
    }
}
