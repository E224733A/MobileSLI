using Microsoft.Maui.Controls;
using TourneesMobile;
using TourneesMobile.ViewModels;

namespace TourneesMobile.Pages;

public partial class ChargementPage : ContentPage
{
    private ChargementViewModel ViewModel => (ChargementViewModel)BindingContext;

    public ChargementPage()
    {
        InitializeComponent();
        BindingContext = AppServices.Get<ChargementViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.InitializeCommand.ExecuteAsync(null);
    }
}
