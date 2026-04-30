using Microsoft.Maui.Controls;
using TourneesMobile;
using TourneesMobile.ViewModels;

namespace TourneesMobile.Pages;

public partial class FinTourneePage : ContentPage
{
    private FinTourneeViewModel ViewModel => (FinTourneeViewModel)BindingContext;

    public FinTourneePage()
    {
        InitializeComponent();
        BindingContext = AppServices.Get<FinTourneeViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.InitializeCommand.ExecuteAsync(null);
    }
}
