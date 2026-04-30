using Microsoft.Maui.Controls;
using TourneesMobile;
using TourneesMobile.ViewModels;

namespace TourneesMobile.Pages;

public partial class TourneeJourPage : ContentPage
{
    private TourneeJourViewModel ViewModel => (TourneeJourViewModel)BindingContext;

    public TourneeJourPage()
    {
        InitializeComponent();
        BindingContext = AppServices.Get<TourneeJourViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.InitializeCommand.ExecuteAsync(null);
    }
}
