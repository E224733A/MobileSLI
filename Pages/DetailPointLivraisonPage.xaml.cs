using Microsoft.Extensions.DependencyInjection;
using MobileSLI.ViewModels;

namespace MobileSLI.Pages;

public partial class DetailPointLivraisonPage : ContentPage
{
    private DetailPointLivraisonViewModel ViewModel => (DetailPointLivraisonViewModel)BindingContext;

    public DetailPointLivraisonPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<DetailPointLivraisonViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadAsync();
    }
}
