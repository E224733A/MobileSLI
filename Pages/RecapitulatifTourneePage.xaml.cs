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
}
