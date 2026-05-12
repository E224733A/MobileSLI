using Microsoft.Extensions.DependencyInjection;
using MobileSLI.ViewModels;

namespace MobileSLI.Pages;

public partial class AccueilPage : ContentPage
{
    private AccueilViewModel ViewModel => (AccueilViewModel)BindingContext;

    public AccueilPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<AccueilViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await ViewModel.CheckActiveTourneeOnStartupAsync();
    }
}
