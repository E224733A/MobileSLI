using Microsoft.Extensions.DependencyInjection;
using MobileSLI.ViewModels;

namespace MobileSLI.Pages;

public partial class IdentificationLivreurPage : ContentPage
{
    private IdentificationLivreurViewModel ViewModel =>
        (IdentificationLivreurViewModel)BindingContext;

    public IdentificationLivreurPage()
    {
        InitializeComponent();

        BindingContext = MauiProgram.Services.GetRequiredService<IdentificationLivreurViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await ViewModel.LoadLivreursAsync();
        }
        catch (Exception exception)
        {
            ViewModel.ErrorMessage = $"Impossible de charger les livreurs : {exception.Message}";

            await DisplayAlertAsync(
                "Chargement des livreurs impossible",
                ViewModel.ErrorMessage,
                "OK");
        }
    }
}
