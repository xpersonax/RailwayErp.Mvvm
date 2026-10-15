using RailwayErp.MvvmDemo.ViewModels;

namespace RailwayErp.MvvmDemo.Views;

public partial class TrainDetailsPage : ContentPage
{
    public TrainDetailsPage(TrainDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
