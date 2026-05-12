using Microsoft.Extensions.DependencyInjection;
using MobileSLI.ViewModels;

namespace MobileSLI.Pages;

public partial class RecapitulatifTourneePage : ContentPage
{
    private RecapitulatifTourneeViewModel ViewModel => (RecapitulatifTourneeViewModel)BindingContext;

    public RecapitulatifTourneePage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<RecapitulatifTourneeViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        // Le retour doit être contrôlé par les boutons de l'application
        // afin d'éviter les sorties accidentelles avant synchronisation.
        return true;
    }
}
