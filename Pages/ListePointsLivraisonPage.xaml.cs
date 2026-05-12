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

    protected override bool OnBackButtonPressed()
    {
        // Page critique : on bloque le retour Android.
        // La navigation doit passer par les boutons visibles de l'application.
        return true;
    }
}
