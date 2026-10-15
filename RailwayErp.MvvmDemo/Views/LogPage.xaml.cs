using RailwayErp.MvvmDemo.ViewModels;

namespace RailwayErp.MvvmDemo.Views;

public partial class LogPage : ContentPage
{
    private readonly LogViewModel _viewModel;

    public LogPage(LogViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }
}
