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

        await ViewModel.LoadLivreursAsync();
    }
}