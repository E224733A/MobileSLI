using Microsoft.Maui.Controls;
using TourneesMobile;
using TourneesMobile.ViewModels;

namespace TourneesMobile.Pages;

public partial class ListeArretsPage : ContentPage
{
    private ListeArretsViewModel ViewModel => (ListeArretsViewModel)BindingContext;

    public ListeArretsPage()
    {
        InitializeComponent();
        BindingContext = AppServices.Get<ListeArretsViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.InitializeCommand.ExecuteAsync(null);
    }
}
