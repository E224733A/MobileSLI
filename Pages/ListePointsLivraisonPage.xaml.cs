using Microsoft.Extensions.DependencyInjection;
using MobileSLI.ViewModels;

namespace MobileSLI.Pages;

public partial class ListePointsLivraisonPage : ContentPage
{
    private ListePointsLivraisonViewModel ViewModel => (ListePointsLivraisonViewModel)BindingContext;

    public ListePointsLivraisonPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<ListePointsLivraisonViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadAsync();
    }
}
