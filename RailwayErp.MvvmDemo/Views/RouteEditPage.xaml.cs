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
        await _viewModel.OnAppearingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }
}
