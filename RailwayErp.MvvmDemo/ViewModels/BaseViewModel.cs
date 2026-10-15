using CommunityToolkit.Mvvm.ComponentModel;

namespace RailwayErp.MvvmDemo.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    protected BaseViewModel(string title)
    {
        Title = title;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }

    public bool IsNotBusy => !IsBusy;
}
